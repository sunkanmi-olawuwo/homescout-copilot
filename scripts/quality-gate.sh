#!/usr/bin/env bash
#
# quality-gate.sh
#
# Runs every currently-active quality-gate check locally so contributors get the
# same result as CI. Additive: new checks are added here as each phase lands.
#
# Usage: scripts/quality-gate.sh
#
set -uo pipefail

cd "$(dirname "$0")/.." || exit 2

FAILED=0
step() { echo; echo "==================== $1 ===================="; }
mark() { if [[ "$1" -ne 0 ]]; then echo ">> FAILED: $2"; FAILED=1; fi; }

# Resolve the solution path (root .sln now; dotnet/*.slnx after Phase 2).
if [[ -f dotnet/HomeScoutCopilot.slnx ]]; then
  SLN="dotnet/HomeScoutCopilot.slnx"
elif [[ -f HomeScoutCopilot.sln ]]; then
  SLN="HomeScoutCopilot.sln"
else
  SLN=""
fi

step "Plan drift"
bash scripts/check-plan-drift.sh
mark $? "plan drift"

step "Backend build + test (fast, non-integration)"
if command -v dotnet >/dev/null 2>&1 && [[ -n "$SLN" ]]; then
  dotnet test "$SLN" --filter "Category!=Integration" --nologo
  mark $? "backend tests"
else
  echo ">> skipped (dotnet or solution not found)"
fi

step "Frontend build + lint + unit test"
if command -v npm >/dev/null 2>&1 && [[ -d frontend ]]; then
  ( cd frontend && npm run build && npm run lint && npm run test )
  mark $? "frontend build/lint/test"
else
  echo ">> skipped (npm or frontend/ not found)"
fi

step "Frontend e2e (Playwright)"
if command -v npm >/dev/null 2>&1 && [[ -d frontend/node_modules/@playwright/test ]]; then
  ( cd frontend && npx playwright install chromium >/dev/null 2>&1 && npm run e2e )
  mark $? "frontend e2e"
else
  echo ">> skipped (@playwright/test not installed — run 'npm i' in frontend/)"
fi

echo
echo "------------------------------------------------------------"
if [[ "$FAILED" -eq 0 ]]; then
  echo "Quality gate: PASS"
  exit 0
fi
echo "Quality gate: FAIL"
exit 1
