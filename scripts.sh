#!/bin/bash

# Pretty output helpers (colors + emojis)
if [ -t 1 ] && command -v tput >/dev/null 2>&1 && [ -n "$(tput colors 2>/dev/null)" ] && [ "$(tput colors 2>/dev/null)" -ge 8 ]; then
	BOLD="\033[1m"; DIM="\033[2m"; RESET="\033[0m"
	RED="\033[31m"; GREEN="\033[32m"; YELLOW="\033[33m"; BLUE="\033[34m"; MAGENTA="\033[35m"; CYAN="\033[36m"
else
	BOLD=""; DIM=""; RESET=""; RED=""; GREEN=""; YELLOW=""; BLUE=""; MAGENTA=""; CYAN=""
fi

ICON_INFO="ℹ️ "
ICON_OK="✅ "
ICON_FAIL="❌ "
ICON_WARN="⚠️ "
ICON_TEST="🧪 "
ICON_RUN="🚀 "
ICON_DOT="• "

say_info() { echo -e "${BLUE}${ICON_INFO}$*${RESET}"; }
say_ok()   { echo -e "${GREEN}${ICON_OK}$*${RESET}"; }
say_warn() { echo -e "${YELLOW}${ICON_WARN}$*${RESET}"; }
say_fail() { echo -e "${RED}${ICON_FAIL}$*${RESET}"; }
section()  { echo -e "\n${BOLD}${MAGENTA}==== $* ====${RESET}\n"; }

usage() {
	echo -e "${BOLD}Usage${RESET}: $0 ${CYAN}[test|run]${RESET} ${YELLOW}[integration|unit|dev|prod]${RESET} ${YELLOW}[watch]${RESET}"
	echo -e "  ${ICON_DOT}${CYAN}test${RESET} ${YELLOW}integration${RESET}/${YELLOW}unit${RESET} ${DIM}- run tests by project path${RESET}"
	echo -e "     ${DIM}• integration → tests/Noo.IntegrationTests${RESET}"
	echo -e "     ${DIM}• unit        → tests/Noo.UnitTests${RESET}"
	echo -e "  ${ICON_DOT}${CYAN}run${RESET}  ${YELLOW}dev${RESET}/${YELLOW}prod${RESET} ${YELLOW}[watch]${RESET} ${DIM}- start src/Noo.Api in specified mode${RESET}"
	echo -e "     ${DIM}• watch supports only run dev (aliases: --watch, -w)${RESET}"
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

ACTION=$1
TARGET=$2
OPTION=$3

run_app() {
	local mode="$1"
	local option="$2"
	local project="src/Noo.Api/Noo.Api.csproj"
	local useWatch=false
	if [ ! -f "$project" ]; then
		say_fail "Project not found: ${project}"
		return 1
	fi

	case "$option" in
		"") ;;
		watch|--watch|-w) useWatch=true ;;
		*)
			say_fail "Unknown run option: ${option}"
			usage
			return 1
			;;
	esac

	case "$mode" in
		dev)  envName="Development" ;;
		prod) envName="Production"  ;;
		*)    say_fail "Unknown run target: ${mode:-<missing>}"; usage; return 1 ;;
	esac

	if [ "$useWatch" = true ] && [ "$mode" != "dev" ]; then
		say_fail "Watch option is available only in dev mode"
		usage
		return 1
	fi

	section "${ICON_RUN} Starting Noo.Api (${envName})"
	say_info "Project: ${project}"
	say_info "Environment: ${envName}"
	if [ "$useWatch" = true ]; then
		say_info "Run mode: watch"
	fi
	say_warn "Press Ctrl+C to stop"
	if [ "$useWatch" = true ]; then
		ASPNETCORE_ENVIRONMENT="$envName" DOTNET_ENVIRONMENT="$envName" \
			dotnet watch --project "$project" run
	else
		ASPNETCORE_ENVIRONMENT="$envName" DOTNET_ENVIRONMENT="$envName" \
			dotnet run --project "$project"
	fi
}

rebuild_app() {
    local project="src/Noo.Api/Noo.Api.csproj"
    if [ ! -f "$project" ]; then
        say_warn "App project not found: ${project}; skipping rebuild."
        return 0
    fi

    section "${ICON_BUILD} Rebuilding Noo.Api"
    say_info "Project: ${project}"
    if dotnet build "$project" -c Release; then
        say_ok "Build succeeded"
        return 0
    else
        say_fail "Build failed"
        return 1
    fi
}

make_temp_file() {
	if command -v mktemp >/dev/null 2>&1; then
		mktemp 2>/dev/null || echo "$SCRIPT_DIR/.tmp_test_$$.log"
	else
		echo "$SCRIPT_DIR/.tmp_test_$$.log"
	fi
}

humanize_seconds() {
	local s=$1
	if [ -z "$s" ] || [ "$s" -lt 0 ] 2>/dev/null; then echo "n/a"; return; fi
	local h=$((s/3600)) m=$(((s%3600)/60)) sec=$((s%60))
	if [ $h -gt 0 ]; then printf "%dh %dm %ds" $h $m $sec; elif [ $m -gt 0 ]; then printf "%dm %ds" $m $sec; else printf "%ds" $sec; fi
}

print_test_summary() {
	# args: kind project output_file rc elapsed_secs
	local kind="$1" project="$2" out="$3" rc="$4" elapsed="$5"
	local total="?" passed="?" failed="?" skipped="?" reported_dur=""

		# Prefer a consolidated "Test summary:" line if present
	local sum_line
	sum_line=$(grep -m1 -E '^Test summary:' "$out" || true)
	if [ -n "$sum_line" ]; then
		# Example: Test summary: total: 12, failed: 1, succeeded: 11, skipped: 0, duration: 1,2m
		read -r total failed passed skipped reported_dur <<<"$(echo "$sum_line" | sed -E 's/.*total: *([0-9]+).*failed: *([0-9]+).*succeeded: *([0-9]+).*skipped: *([0-9]+).*duration: *(.+)/\1 \2 \3 \4 \5/' 2>/dev/null)"
	else
			# Try the modern dotnet summary: "Failed!/Passed!  - Failed: X, Passed: Y, Skipped: Z, Total: N, Duration: ... - <dll>"
			local dn_line
			dn_line=$(grep -m1 -E '^(Passed|Failed|Skipped)!\s*-\s*Failed:' "$out" || true)
			if [ -n "$dn_line" ]; then
				failed=$(echo "$dn_line" | sed -E 's/.*Failed: *([0-9]+).*/\1/' 2>/dev/null)
				passed=$(echo "$dn_line" | sed -E 's/.*Passed: *([0-9]+).*/\1/' 2>/dev/null)
				skipped=$(echo "$dn_line" | sed -E 's/.*Skipped: *([0-9]+).*/\1/' 2>/dev/null)
				total=$(echo "$dn_line" | sed -E 's/.*Total: *([0-9]+).*/\1/' 2>/dev/null)
				# Capture up to the trailing " - <dll>" if present
				reported_dur=$(echo "$dn_line" | sed -E 's/.*Duration: *([^,\-][^\-]*[^ ])(\s*-\s*.*)?/\1/' 2>/dev/null)
			else
				# Try the VSTest summary format
				local vs_line
				vs_line=$(grep -m1 -E '^Total tests:' "$out" || true)
				if [ -n "$vs_line" ]; then
					# Example: Total tests: 12. Passed: 11. Failed: 1. Skipped: 0.
					read -r total passed failed skipped <<<"$(echo "$vs_line" | sed -E 's/Total tests: *([0-9]+)\..*Passed: *([0-9]+)\..*Failed: *([0-9]+)\..*Skipped: *([0-9]+).*/\1 \2 \3 \4/' 2>/dev/null)"
					# Duration line variants
					reported_dur=$(grep -m1 -E 'Total time:|Test execution time:' "$out" | sed -E 's/.*(Total time:|Test execution time:) *//' || true)
				fi
			fi
	fi

	local status_icon status_text status_color
	if [ "$rc" -eq 0 ]; then
		status_icon="$ICON_OK"; status_text="PASSED"; status_color="$GREEN"
	else
		status_icon="$ICON_FAIL"; status_text="FAILED"; status_color="$RED"
	fi

	local elapsed_human; elapsed_human=$(humanize_seconds "$elapsed")

	echo -e "${BOLD}${MAGENTA}────────────────────────────────────────────${RESET}"
	echo -e "${status_color}${status_icon}${BOLD}${kind^} tests ${status_text}${RESET}  ${DIM}(${project})${RESET}"
	echo -e "${BOLD}${MAGENTA}────────────────────────────────────────────${RESET}"
	echo -e "${GREEN}✅ Passed:${RESET}  ${BOLD}${passed}${RESET}    ${RED}❌ Failed:${RESET}  ${BOLD}${failed}${RESET}    ${YELLOW}⚠️  Skipped:${RESET} ${BOLD}${skipped}${RESET}    ${CYAN}ℹ️  Total:${RESET} ${BOLD}${total}${RESET}"
	if [ -n "$reported_dur" ]; then
		echo -e "${BLUE}🕒 Reported duration:${RESET} ${BOLD}${reported_dur}${RESET}"
	fi
	echo -e "${BLUE}🕒 Elapsed:${RESET} ${BOLD}${elapsed_human}${RESET}\n"
}

run_tests_by_project() {
	local kind="$1"
	local project=""
	case "$kind" in
		integration) project="tests/Noo.IntegrationTests" ;;
		unit)        project="tests/Noo.UnitTests" ;;
		*)
			say_fail "Unknown test target: ${kind:-<missing>}"
			usage; return 1 ;;
	esac

	if [ ! -d "$project" ]; then
		say_fail "Test project folder not found: ${project}"
		return 1
	fi

	# Rebuild the app before running tests
    rebuild_app || { say_fail "Aborting tests due to build failure."; return 1; }

	section "${ICON_TEST} Running ${kind} tests"
	say_info "Project: ${project}"

	local tmp; tmp=$(make_temp_file)
	local start_ts end_ts elapsed rc
	start_ts=$(date +%s)
	# Capture output while preserving original exit code
	set +o pipefail 2>/dev/null || true
	dotnet test "$project" -c Release --logger "trx" | tee "$tmp"
	rc=${PIPESTATUS[0]:-${?}}
	end_ts=$(date +%s)
	elapsed=$((end_ts-start_ts))

	print_test_summary "$kind" "$project" "$tmp" "$rc" "$elapsed"
	rm -f "$tmp" 2>/dev/null || true
	return $rc
}

case "$ACTION" in
		test)
			run_tests_by_project "$TARGET"
			exit $?
			;;
	run)
		run_app "$TARGET" "$OPTION"
		;;
	*)
		say_warn "Missing or unknown command: ${ACTION:-<missing>}"
		usage
		exit 1
		;;
esac
