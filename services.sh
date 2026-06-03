#!/usr/bin/env python3
"""
services.sh — start the local dev services for noo-api.

Brings up everything the API expects on localhost:
  • MySQL          (verified, not started — should be a system service)
  • Redis          (cache, :6379)
  • Mailpit        (SMTP :1025, web UI :8025)
  • MinIO          (S3-compatible, API :9000, console :9001)
  • Aspire Dash    (OpenTelemetry UI :18888, OTLP/gRPC :4317, OTLP/HTTP :4318)

S3 credentials/bucket and the Redis port are read from
src/Noo.Api/appsettings.Development.json so the script stays in sync
with the app config.

Usage:
    ./services.sh             # run in foreground, Ctrl+C stops everything
    ./services.sh --no-redis  # skip Redis
    ./services.sh --no-minio  # skip MinIO
    ./services.sh --no-mail   # skip mailpit
    ./services.sh --no-otel   # skip Aspire Dashboard

Requires Python 3.10+, curl. Pure stdlib otherwise.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import signal
import socket
import subprocess
import sys
import threading
import time
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import IO, Optional

# ─── paths & config ──────────────────────────────────────────────────────────

ROOT = Path(__file__).resolve().parent
APPSETTINGS = ROOT / "src" / "Noo.Api" / "appsettings.Development.json"

LOCAL_BIN = Path.home() / ".local" / "bin"
DATA_DIR = Path.home() / ".local" / "share" / "noo-services"
MINIO_DATA = DATA_DIR / "minio-data"
REDIS_DATA = DATA_DIR / "redis-data"
LOG_DIR = DATA_DIR / "logs"

MINIO_API_PORT = 9000
MINIO_CONSOLE_PORT = 9001
MYSQL_PORT = 3306
REDIS_PORT = 6379  # overridden by appsettings Cache.ConnectionString

ASPIRE_DASHBOARD_UI_PORT = 18888
ASPIRE_OTLP_GRPC_PORT = 4317
ASPIRE_OTLP_HTTP_PORT = 4318
ASPIRE_BIN_FALLBACK = Path.home() / ".aspire" / "bin"

MINIO_DOWNLOAD = "https://dl.min.io/server/minio/release/linux-amd64/minio"
MC_DOWNLOAD = "https://dl.min.io/client/mc/release/linux-amd64/mc"
ASPIRE_INSTALL_URL = "https://aspire.dev/install.sh"

# ─── pretty output ───────────────────────────────────────────────────────────

USE_COLOR = sys.stdout.isatty() and os.environ.get("NO_COLOR") is None

class C:
    RESET = "\033[0m" if USE_COLOR else ""
    BOLD = "\033[1m" if USE_COLOR else ""
    DIM = "\033[2m" if USE_COLOR else ""
    RED = "\033[31m" if USE_COLOR else ""
    GREEN = "\033[32m" if USE_COLOR else ""
    YELLOW = "\033[33m" if USE_COLOR else ""
    BLUE = "\033[34m" if USE_COLOR else ""
    MAGENTA = "\033[35m" if USE_COLOR else ""
    CYAN = "\033[36m" if USE_COLOR else ""
    GRAY = "\033[90m" if USE_COLOR else ""

PRINT_LOCK = threading.Lock()

def _stamp() -> str:
    return time.strftime("%H:%M:%S")

def info(msg: str) -> None:
    with PRINT_LOCK:
        print(f"{C.GRAY}{_stamp()}{C.RESET} {C.BLUE}ℹ {C.RESET}{msg}", flush=True)

def ok(msg: str) -> None:
    with PRINT_LOCK:
        print(f"{C.GRAY}{_stamp()}{C.RESET} {C.GREEN}✓ {C.RESET}{msg}", flush=True)

def warn(msg: str) -> None:
    with PRINT_LOCK:
        print(f"{C.GRAY}{_stamp()}{C.RESET} {C.YELLOW}! {C.RESET}{msg}", flush=True)

def fail(msg: str) -> None:
    with PRINT_LOCK:
        print(f"{C.GRAY}{_stamp()}{C.RESET} {C.RED}✗ {C.RESET}{msg}", flush=True)

def banner(msg: str) -> None:
    bar = "─" * (len(msg) + 4)
    with PRINT_LOCK:
        print(f"\n{C.CYAN}{C.BOLD}{bar}\n  {msg}\n{bar}{C.RESET}", flush=True)

# ─── helpers ─────────────────────────────────────────────────────────────────

def _strip_jsonc_comments(text: str) -> str:
    """Strip // and /* */ comments while leaving string literals untouched."""
    out: list[str] = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '"':
            j = i + 1
            while j < n:
                if text[j] == "\\" and j + 1 < n:
                    j += 2
                    continue
                if text[j] == '"':
                    j += 1
                    break
                j += 1
            out.append(text[i:j])
            i = j
        elif c == "/" and i + 1 < n and text[i + 1] == "/":
            j = i + 2
            while j < n and text[j] != "\n":
                j += 1
            i = j
        elif c == "/" and i + 1 < n and text[i + 1] == "*":
            j = i + 2
            while j + 1 < n and not (text[j] == "*" and text[j + 1] == "/"):
                j += 1
            i = j + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)

def load_s3_config() -> dict:
    """Read S3 creds from appsettings.Development.json (tolerant of // and /* */ comments)."""
    if not APPSETTINGS.exists():
        fail(f"appsettings.Development.json not found at {APPSETTINGS}")
        sys.exit(1)
    raw = APPSETTINGS.read_text(encoding="utf-8")
    cleaned = _strip_jsonc_comments(raw)
    data = json.loads(cleaned)
    s3 = data.get("S3") or {}
    missing = [k for k in ("BucketName", "AccessKey", "SecretKey") if not s3.get(k)]
    if missing:
        fail(f"S3 config missing keys: {', '.join(missing)}")
        sys.exit(1)
    return s3

def load_redis_port() -> int:
    """Parse the Redis port from appsettings Cache.ConnectionString (host:port)."""
    if not APPSETTINGS.exists():
        return REDIS_PORT
    cleaned = _strip_jsonc_comments(APPSETTINGS.read_text(encoding="utf-8"))
    cache = json.loads(cleaned).get("Cache") or {}
    conn = (cache.get("ConnectionString") or "").split(",")[0].strip()
    if ":" in conn:
        try:
            return int(conn.rsplit(":", 1)[1])
        except ValueError:
            pass
    return REDIS_PORT

def port_open(host: str, port: int, timeout: float = 0.5) -> bool:
    try:
        with socket.create_connection((host, port), timeout=timeout):
            return True
    except OSError:
        return False

def wait_for_port(host: str, port: int, timeout: float = 30.0) -> bool:
    deadline = time.time() + timeout
    while time.time() < deadline:
        if port_open(host, port):
            return True
        time.sleep(0.25)
    return False

def which(name: str) -> Optional[str]:
    p = shutil.which(name)
    if p:
        return p
    candidate = LOCAL_BIN / name
    if candidate.exists() and os.access(candidate, os.X_OK):
        return str(candidate)
    return None

def download(url: str, dest: Path) -> None:
    info(f"downloading {url}")
    dest.parent.mkdir(parents=True, exist_ok=True)
    tmp = dest.with_suffix(dest.suffix + ".part")
    with urllib.request.urlopen(url) as r, tmp.open("wb") as f:
        shutil.copyfileobj(r, f)
    tmp.chmod(0o755)
    tmp.replace(dest)
    ok(f"installed → {dest}")

# ─── service runner ──────────────────────────────────────────────────────────

@dataclass
class Service:
    name: str
    color: str
    proc: subprocess.Popen
    log_file: Path

SERVICES: list[Service] = []
SHUTDOWN = threading.Event()

def _stream(name: str, color: str, stream: IO[bytes], log_file: Path) -> None:
    prefix = f"{color}{name:>8}{C.RESET} {C.GRAY}│{C.RESET} "
    with log_file.open("ab") as lf:
        for line in iter(stream.readline, b""):
            lf.write(line)
            lf.flush()
            text = line.decode("utf-8", errors="replace").rstrip("\n")
            with PRINT_LOCK:
                print(prefix + text, flush=True)
    stream.close()

def spawn(name: str, color: str, cmd: list[str], env: Optional[dict] = None) -> Service:
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    log_file = LOG_DIR / f"{name}.log"
    info(f"starting {C.BOLD}{name}{C.RESET}: {C.DIM}{' '.join(cmd)}{C.RESET}")
    proc = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        env={**os.environ, **(env or {})},
        bufsize=0,
    )
    threading.Thread(
        target=_stream, args=(name, color, proc.stdout, log_file), daemon=True
    ).start()
    svc = Service(name=name, color=color, proc=proc, log_file=log_file)
    SERVICES.append(svc)
    return svc

def shutdown(_signum=None, _frame=None) -> None:
    if SHUTDOWN.is_set():
        return
    SHUTDOWN.set()
    print()
    banner("stopping services")
    for svc in SERVICES:
        if svc.proc.poll() is None:
            info(f"sending SIGTERM to {svc.name} (pid {svc.proc.pid})")
            try:
                svc.proc.terminate()
            except ProcessLookupError:
                pass
    deadline = time.time() + 8
    for svc in SERVICES:
        remaining = max(0.1, deadline - time.time())
        try:
            svc.proc.wait(timeout=remaining)
            ok(f"{svc.name} stopped")
        except subprocess.TimeoutExpired:
            warn(f"{svc.name} ignored SIGTERM — killing")
            svc.proc.kill()
            svc.proc.wait()

# ─── prereq checks ───────────────────────────────────────────────────────────

def check_mysql() -> None:
    banner("MySQL")
    active = False
    if shutil.which("systemctl"):
        for unit in ("mysqld", "mariadb", "mysql"):
            r = subprocess.run(
                ["systemctl", "is-active", "--quiet", unit],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
            if r.returncode == 0:
                ok(f"systemd unit {C.BOLD}{unit}{C.RESET} is active")
                active = True
                break
    if port_open("127.0.0.1", MYSQL_PORT):
        ok(f"port {MYSQL_PORT} is open on localhost")
        return
    if active:
        warn(f"unit is active but port {MYSQL_PORT} is not reachable yet")
        if wait_for_port("127.0.0.1", MYSQL_PORT, timeout=10):
            ok(f"port {MYSQL_PORT} now open")
            return
    fail(f"MySQL is not reachable on localhost:{MYSQL_PORT}")
    fail("start it with:  sudo systemctl start mysqld")
    sys.exit(1)

def ensure_mailpit() -> str:
    if path := which("mailpit"):
        return path
    fail("mailpit not found in PATH and no auto-installer here")
    fail("install: https://mailpit.axllent.org/docs/install/")
    sys.exit(1)

def ensure_redis() -> str:
    if path := which("redis-server"):
        return path
    if path := which("valkey-server"):  # Fedora ships valkey, a drop-in Redis fork
        return path
    fail("redis-server not found in PATH and no auto-installer here")
    fail("install on Fedora:  sudo dnf install redis   (or valkey)")
    sys.exit(1)

def ensure_minio() -> str:
    if path := which("minio"):
        return path
    warn("minio not installed — fetching")
    LOCAL_BIN.mkdir(parents=True, exist_ok=True)
    download(MINIO_DOWNLOAD, LOCAL_BIN / "minio")
    return str(LOCAL_BIN / "minio")

def ensure_mc() -> str:
    if path := which("mc"):
        return path
    warn("mc (MinIO client) not installed — fetching")
    LOCAL_BIN.mkdir(parents=True, exist_ok=True)
    download(MC_DOWNLOAD, LOCAL_BIN / "mc")
    return str(LOCAL_BIN / "mc")

def find_aspire() -> Optional[str]:
    if path := which("aspire"):
        return path
    candidate = ASPIRE_BIN_FALLBACK / "aspire"
    if candidate.exists() and os.access(candidate, os.X_OK):
        return str(candidate)
    return None

def ensure_aspire_cli() -> str:
    if path := find_aspire():
        return path
    warn("aspire CLI not installed — running official installer")
    info(f"installer: {ASPIRE_INSTALL_URL}")
    rc = subprocess.run(
        f"curl -sSL {ASPIRE_INSTALL_URL} | bash",
        shell=True,
        check=False,
    )
    if rc.returncode != 0:
        fail("aspire CLI installer failed")
        sys.exit(1)
    if path := find_aspire():
        ok(f"aspire installed → {path}")
        return path
    fail(f"aspire CLI installed but not found in PATH or {ASPIRE_BIN_FALLBACK}")
    fail("Open a new shell so the installer's PATH update takes effect, or rerun.")
    sys.exit(1)

# ─── service starters ────────────────────────────────────────────────────────

def start_redis(port: int) -> None:
    banner("Redis")
    if port_open("127.0.0.1", port):
        ok(f"already running on :{port}")
        return
    bin_path = ensure_redis()
    REDIS_DATA.mkdir(parents=True, exist_ok=True)
    spawn(
        "redis",
        C.RED,
        [
            bin_path,
            "--port", str(port),
            "--bind", "127.0.0.1",
            "--dir", str(REDIS_DATA),
            # ephemeral dev cache: keep it light, no AOF, lazy RDB snapshots only
            "--appendonly", "no",
            "--save", "",
        ],
    )
    if wait_for_port("127.0.0.1", port, timeout=10):
        ok(f"ready on :{port}")
    else:
        warn(f"redis didn't open :{port} within 10s — check the log above")

def start_mailpit() -> None:
    banner("Mailpit")
    bin_path = ensure_mailpit()
    spawn(
        "mailpit",
        C.MAGENTA,
        [bin_path, "--smtp-auth-accept-any", "--smtp-auth-allow-insecure", "--verbose"],
    )
    if wait_for_port("127.0.0.1", 1025, timeout=10):
        ok("SMTP ready on :1025  •  UI: http://localhost:8025")
    else:
        warn("mailpit didn't open :1025 within 10s — check the log above")

def start_minio(s3: dict) -> None:
    banner("MinIO")
    bin_path = ensure_minio()
    mc_path = ensure_mc()
    MINIO_DATA.mkdir(parents=True, exist_ok=True)

    env = {
        "MINIO_ROOT_USER": s3["AccessKey"],
        "MINIO_ROOT_PASSWORD": s3["SecretKey"],
        # Allow browser PUTs from the SPA against presigned URLs.
        # "*" is fine for local dev; tighten in real environments.
        "MINIO_API_CORS_ALLOW_ORIGIN": "*",
    }
    spawn(
        "minio",
        C.YELLOW,
        [
            bin_path,
            "server",
            str(MINIO_DATA),
            "--address", f":{MINIO_API_PORT}",
            "--console-address", f":{MINIO_CONSOLE_PORT}",
        ],
        env=env,
    )
    if not wait_for_port("127.0.0.1", MINIO_API_PORT, timeout=20):
        fail(f"minio failed to open :{MINIO_API_PORT}")
        return
    ok(f"S3 API on :{MINIO_API_PORT}  •  console: http://localhost:{MINIO_CONSOLE_PORT}")
    ensure_bucket(mc_path, s3)

def start_aspire_dashboard() -> None:
    banner("Aspire Dashboard (OpenTelemetry)")
    if port_open("127.0.0.1", ASPIRE_DASHBOARD_UI_PORT):
        ok(f"already running on :{ASPIRE_DASHBOARD_UI_PORT}")
        return
    if port_open("127.0.0.1", ASPIRE_OTLP_GRPC_PORT):
        warn(f"port {ASPIRE_OTLP_GRPC_PORT} is in use — skipping Aspire Dashboard")
        return
    bin_path = ensure_aspire_cli()
    spawn(
        "aspire",
        C.CYAN,
        [bin_path, "dashboard", "run", "--allow-anonymous"],
        env={"ASPIRE_ALLOW_UNSECURED_TRANSPORT": "true"},
    )
    if wait_for_port("127.0.0.1", ASPIRE_DASHBOARD_UI_PORT, timeout=30):
        ok(
            f"UI: http://localhost:{ASPIRE_DASHBOARD_UI_PORT}  "
            f"•  OTLP/gRPC :{ASPIRE_OTLP_GRPC_PORT}  "
            f"•  OTLP/HTTP :{ASPIRE_OTLP_HTTP_PORT}"
        )
    else:
        warn(f"aspire dashboard didn't open :{ASPIRE_DASHBOARD_UI_PORT} within 30s — check the log above")

def ensure_bucket(mc_path: str, s3: dict) -> None:
    alias = "noo-local"
    bucket = s3["BucketName"]
    endpoint = f"http://127.0.0.1:{MINIO_API_PORT}"

    def run(args: list[str]) -> subprocess.CompletedProcess:
        return subprocess.run(
            [mc_path, *args],
            capture_output=True,
            text=True,
            env={**os.environ, "MC_QUIET": "1"},
        )

    info(f"configuring mc alias '{alias}' → {endpoint}")
    r = run(["alias", "set", alias, endpoint, s3["AccessKey"], s3["SecretKey"]])
    if r.returncode != 0:
        warn(f"mc alias set failed: {r.stderr.strip() or r.stdout.strip()}")
        return

    r = run(["ls", f"{alias}/{bucket}"])
    if r.returncode == 0:
        ok(f"bucket {C.BOLD}{bucket}{C.RESET} already exists")
    else:
        info(f"creating bucket {bucket}")
        r = run(["mb", f"{alias}/{bucket}"])
        if r.returncode == 0:
            ok(f"bucket {C.BOLD}{bucket}{C.RESET} created")
        else:
            fail(f"failed to create bucket: {r.stderr.strip() or r.stdout.strip()}")

# ─── main ────────────────────────────────────────────────────────────────────

def main() -> int:
    ap = argparse.ArgumentParser(description="Start local dev services for noo-api.")
    ap.add_argument("--no-redis", action="store_true", help="skip Redis")
    ap.add_argument("--no-minio", action="store_true", help="skip MinIO")
    ap.add_argument("--no-mail", action="store_true", help="skip mailpit")
    ap.add_argument("--no-otel", action="store_true", help="skip Aspire Dashboard")
    args = ap.parse_args()

    s3 = load_s3_config()
    redis_port = load_redis_port()

    banner("noo-api dev services")
    info(f"project root  : {ROOT}")
    info(f"data dir      : {DATA_DIR}")
    info(f"logs          : {LOG_DIR}")
    info(f"S3 bucket     : {s3['BucketName']} (key: {s3['AccessKey']})")
    info(f"Redis port    : {redis_port}")

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    check_mysql()
    if not args.no_redis:
        start_redis(redis_port)
    if not args.no_mail:
        start_mailpit()
    if not args.no_minio:
        start_minio(s3)
    if not args.no_otel:
        start_aspire_dashboard()

    if not SERVICES:
        warn("no services started")
        return 0

    banner("running — Ctrl+C to stop")
    while not SHUTDOWN.is_set():
        for svc in SERVICES:
            rc = svc.proc.poll()
            if rc is not None and not SHUTDOWN.is_set():
                fail(f"{svc.name} exited unexpectedly (rc={rc}) — see {svc.log_file}")
                shutdown()
                return 1
        time.sleep(0.5)
    return 0

if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        shutdown()
        sys.exit(130)
