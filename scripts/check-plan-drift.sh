#!/usr/bin/env bash
#
# check-plan-drift.sh
#
# Enforces the HomeScout planning invariants so wiki/__plans/ stays internally
# consistent AND the code does not drift away from the plan.
#
# Source of truth:
#   wiki/__plans/00-roadmap/homescout-skeleton-migration-plan.md
#   wiki/__plans/README.md  ("Detecting Plan Drift" section)
#
# Severities:
#   FAIL -> invariant that must never be violated; exits non-zero (blocks CI).
#   WARN -> forward-looking convergence expected to be pending until its phase
#           lands; reported but does not fail the build.
#
# Portable: POSIX `grep -E` only (no ripgrep, no PCRE), so it runs the same on
# macOS (BSD grep) and CI (GNU grep).
#
# Usage:
#   scripts/check-plan-drift.sh            # report FAIL + WARN, exit 1 on any FAIL
#   scripts/check-plan-drift.sh --strict   # also treat WARN as failure
#
set -uo pipefail

cd "$(dirname "$0")/.." || exit 2

PLANS="wiki/__plans"
README="$PLANS/README.md"
STRICT=0
[[ "${1:-}" == "--strict" ]] && STRICT=1

if [[ -t 1 ]]; then
  RED=$'\033[31m'; YEL=$'\033[33m'; GRN=$'\033[32m'; DIM=$'\033[2m'; RST=$'\033[0m'
else
  RED=""; YEL=""; GRN=""; DIM=""; RST=""
fi

FAILS=0
WARNS=0
fail() { echo "${RED}FAIL${RST} $1"; FAILS=$((FAILS + 1)); }
warn() { echo "${YEL}WARN${RST} $1"; WARNS=$((WARNS + 1)); }
pass() { echo "${GRN}ok  ${RST} ${DIM}$1${RST}"; }

report() { # LEVEL HITS MESSAGE
  local level="$1" hits="$2" msg="$3" n
  if [[ -n "$hits" ]]; then
    "$level" "$msg"
    n=$(echo "$hits" | wc -l | tr -d ' ')
    echo "$hits" | head -n 12 | sed 's/^/       /'
    [[ "$n" -gt 12 ]] && echo "       ${DIM}... and $((n - 12)) more${RST}"
  else
    pass "$msg"
  fi
}

echo "== Plan-internal invariants (${PLANS}) =="

# 1. Every plan markdown file (except the top-level README) is indexed in README.md.
missing_index=""
while IFS= read -r f; do
  rel="${f#"$PLANS"/}"
  [[ "$rel" == "README.md" ]] && continue
  grep -qF -- "$rel" "$README" 2>/dev/null || missing_index+="$f not linked in $README"$'\n'
done < <(find "$PLANS" -name '*.md' | sort)
report fail "$(printf '%s' "$missing_index" | grep -v '^$')" \
  "every plan file is indexed in README.md"

# 2. Relative markdown links in README.md resolve to existing files.
broken=""
while IFS= read -r target; do
  [[ -z "$target" ]] && continue
  clean="${target#./}"; clean="${clean%%#*}"
  [[ -e "$PLANS/$clean" || -e "$clean" ]] || broken+="broken link: $target"$'\n'
done < <(grep -oE '\]\([^)]+\.md[^)]*\)' "$README" 2>/dev/null \
          | sed -E 's/^\]\(//; s/\)$//' | sort -u)
report fail "$(printf '%s' "$broken" | grep -v '^$')" \
  "README.md links resolve to existing files"

# 3. The plan restructure must not regress: no active `wiki/plan/` path reference
#    (the canonical root is wiki/__plans/). Historical log entries are exempt.
stale_root="$(grep -rEn 'wiki/plan/' --include='*.md' . 2>/dev/null \
              | grep -v 'wiki/__plans/' | grep -v '^\./wiki/log\.md:')"
report fail "$stale_root" \
  "no active wiki/plan/ path reference (canonical root is wiki/__plans/)"

echo
echo "== Plan-vs-code invariants =="

# 4. API-first: the React frontend must not import an agent/LLM SDK directly.
fe_sdk="$(grep -rEn "from ['\"](openai|@azure/(ai|openai|ai-projects|ai-agents)|@microsoft/foundry)" \
            frontend/src 2>/dev/null)"
report fail "$fe_sdk" \
  "frontend does not import an agent/LLM SDK directly (API-first)"

# 5. Result type is FluentResults, not ErrorOr (locked decision).
erroror="$(grep -rEn '\bErrorOr\b' --include='*.cs' --include='*.csproj' . 2>/dev/null \
            | grep -v '/bin/' | grep -v '/obj/')"
report fail "$erroror" \
  "no ErrorOr usage (the chosen Result type is FluentResults)"

echo
echo "== Plan-vs-code convergence (forward-looking, non-blocking) =="

# 6. FluentResults is introduced in Phase 3; until then no csproj references it.
fluent="$(grep -rEln 'FluentResults' --include='*.csproj' . 2>/dev/null \
           | grep -v '/bin/' | grep -v '/obj/')"
if [[ -z "$fluent" ]]; then
  warn "Phase 3 pending: FluentResults not yet referenced by any project (expected until the layering phase)"
else
  pass "FluentResults is referenced by a project"
fi

echo
echo "------------------------------------------------------------"
echo "Plan drift: ${RED}${FAILS} fail${RST}, ${YEL}${WARNS} warn${RST}"
if [[ $FAILS -gt 0 ]]; then
  echo "${RED}Drift detected. Update the owning plan or the code so they agree.${RST}"
  echo "See ${PLANS}/README.md > 'Detecting Plan Drift' and the master migration plan."
  exit 1
fi
if [[ $STRICT -eq 1 && $WARNS -gt 0 ]]; then
  echo "${YEL}--strict: warnings treated as failures.${RST}"
  exit 1
fi
echo "${GRN}No blocking drift.${RST}"
exit 0
