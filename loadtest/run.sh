#!/bin/bash
set -euo pipefail

if [ -t 1 ] && command -v tput >/dev/null 2>&1 && [ -n "$(tput colors 2>/dev/null)" ] && [ "$(tput colors 2>/dev/null)" -ge 8 ]; then
	BOLD="\033[1m"; DIM="\033[2m"; RESET="\033[0m"
	RED="\033[31m"; GREEN="\033[32m"; YELLOW="\033[33m"; BLUE="\033[34m"; MAGENTA="\033[35m"; CYAN="\033[36m"
else
	BOLD=""; DIM=""; RESET=""; RED=""; GREEN=""; YELLOW=""; BLUE=""; MAGENTA=""; CYAN=""
fi

say_info() { echo -e "${BLUE}ℹ️  $*${RESET}"; }
say_ok()   { echo -e "${GREEN}✅ $*${RESET}"; }
say_warn() { echo -e "${YELLOW}⚠️  $*${RESET}"; }
say_fail() { echo -e "${RED}❌ $*${RESET}"; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCAL_BIN="$HOME/.local/bin"
K6_VERSION="${K6_VERSION:-v1.1.0}"

usage() {
	echo -e "${BOLD}Usage${RESET}: $0 ${CYAN}<student|mentor|teacher|assistant|admin>${RESET} ${YELLOW}[smoke|load|stress]${RESET}"
	echo -e "  ${DIM}smoke  - 1 VU, 30s sanity run${RESET}"
	echo -e "  ${DIM}load   - 10 VUs, 2m steady load (default)${RESET}"
	echo -e "  ${DIM}stress - ramp 0 → 60 VUs over 3m${RESET}"
	echo ""
	echo -e "${BOLD}Environment${RESET}:"
	echo -e "  ${CYAN}NOO_USER${RESET}      login (username or email) of an account with the chosen role ${RED}[required]${RESET}"
	echo -e "  ${CYAN}NOO_PASSWORD${RESET}  password ${RED}[required]${RESET}"
	echo -e "  ${CYAN}BASE_URL${RESET}      API base url ${DIM}(default: http://localhost:5001)${RESET}"
	echo -e "  ${CYAN}K6_VERSION${RESET}    k6 release to install if missing ${DIM}(default: ${K6_VERSION})${RESET}"
}

ensure_k6() {
	if command -v k6 >/dev/null 2>&1; then
		return 0
	fi

	if [ -x "$LOCAL_BIN/k6" ]; then
		PATH="$LOCAL_BIN:$PATH"
		return 0
	fi

	local arch
	case "$(uname -m)" in
		x86_64) arch="amd64" ;;
		aarch64) arch="arm64" ;;
		*) say_fail "Unsupported architecture: $(uname -m)"; return 1 ;;
	esac

	say_info "k6 not found, downloading ${K6_VERSION} to ${LOCAL_BIN}..."
	local tmp
	tmp="$(mktemp -d)"
	local name="k6-${K6_VERSION}-linux-${arch}"
	curl -fsSL "https://github.com/grafana/k6/releases/download/${K6_VERSION}/${name}.tar.gz" -o "$tmp/k6.tar.gz"
	tar -xzf "$tmp/k6.tar.gz" -C "$tmp"
	mkdir -p "$LOCAL_BIN"
	mv "$tmp/$name/k6" "$LOCAL_BIN/k6"
	chmod +x "$LOCAL_BIN/k6"
	rm -rf "$tmp"
	PATH="$LOCAL_BIN:$PATH"
	say_ok "k6 installed: $(k6 version | head -1)"
}

ROLE="${1:-}"
PROFILE="${2:-load}"

case "$ROLE" in
	student|mentor|teacher|assistant|admin) ;;
	-h|--help) usage; exit 0 ;;
	"") say_fail "Missing role"; usage; exit 1 ;;
	*) say_fail "Unknown role: $ROLE"; usage; exit 1 ;;
esac

case "$PROFILE" in
	smoke|load|stress) ;;
	*) say_fail "Unknown profile: $PROFILE"; usage; exit 1 ;;
esac

if [ -z "${NOO_USER:-}" ] || [ -z "${NOO_PASSWORD:-}" ]; then
	say_fail "NOO_USER and NOO_PASSWORD must be set"
	usage
	exit 1
fi

BASE_URL="${BASE_URL:-http://localhost:5001}"

ensure_k6

if ! curl -fsS -m 3 -o /dev/null "$BASE_URL/healthz" 2>/dev/null && ! curl -fsS -m 3 -o /dev/null "$BASE_URL/health" 2>/dev/null; then
	say_warn "API does not respond on ${BASE_URL} — is it running? (./scripts.sh run dev)"
fi

say_info "Role:     ${ROLE}"
say_info "Profile:  ${PROFILE}"
say_info "Base URL: ${BASE_URL}"
say_warn "The API rate-limits to 300 req/min per IP by default. For anything beyond smoke, start it with:"
say_warn "  RateLimiting__Global__PermitLimit=1000000 ./scripts.sh run dev"
echo ""

exec k6 run \
	-e BASE_URL="$BASE_URL" \
	-e NOO_USER="$NOO_USER" \
	-e NOO_PASSWORD="$NOO_PASSWORD" \
	-e PROFILE="$PROFILE" \
	"$SCRIPT_DIR/$ROLE.js"
