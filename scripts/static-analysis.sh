#!/usr/bin/env bash
#
# static-analysis.sh
#
# Local static analysis for HomeScout, spanning every surface the (now-dormant)
# CodeQL workflow covered: the .NET backend, the React/TypeScript frontend, and the
# GitHub Actions workflows themselves. It is the single source of truth invoked by
# humans, by CI (.github/workflows/static-analysis.yml), and by any coding agent
# (Claude via .claude/skills/static-analysis, Codex/Copilot via AGENTS.md).
#
# Tools (each self-skips with an install hint when absent):
#   - Lizard        cyclomatic complexity for C# and TypeScript  (pipx install lizard)
#   - InspectCode   JetBrains ReSharper static analysis for .NET  (dotnet tool install -g JetBrains.ReSharper.GlobalTools)
#   - actionlint    GitHub Actions workflow linter                (brew install actionlint)
#   - ESLint        frontend smells + complexity — run via `pnpm run lint`, not here
#
# Posture: ADVISORY. Findings are reported but never fail the run, so this is safe in
# the quality gate and on PRs. Pass --strict to make findings exit non-zero (for a
# future blocking gate). Machine-readable output lands in artifacts/static-analysis/
# (gitignored) for CI to upload.
#
# Usage:
#   scripts/static-analysis.sh [all|complexity|inspect|actions] [--strict]
#
set -uo pipefail

cd "$(dirname "$0")/.." || exit 2

# Make locally-installed tools discoverable regardless of the caller's shell setup.
export PATH="$HOME/.local/bin:$HOME/.dotnet/tools:$PATH"

TARGET="${1:-all}"
STRICT=0
for arg in "$@"; do [[ "$arg" == "--strict" ]] && STRICT=1; done

BACKEND_DIR="dotnet"
SOLUTION="dotnet/HomeScoutCopilot.slnx"
FRONTEND_SRC="frontend/src"
OUT_DIR="artifacts/static-analysis"
mkdir -p "$OUT_DIR"

FINDINGS=0   # count of surfaces that reported findings
SKIPPED=0    # count of tools skipped (not installed)

hdr()  { echo; echo "==================== $1 ===================="; }
skip() { echo ">> skipped: $1"; SKIPPED=$((SKIPPED + 1)); }

# Resolve the lizard entry point (pipx/global binary, or the python module fallback).
lizard_cmd() {
  if command -v lizard >/dev/null 2>&1; then echo "lizard"; return 0; fi
  if command -v python3 >/dev/null 2>&1 && python3 -m lizard --version >/dev/null 2>&1; then
    echo "python3 -m lizard"; return 0
  fi
  return 1
}

# --- Complexity: Lizard over C# and TypeScript -------------------------------------
# Default thresholds (CCN>15, length>1000, params>100) are the "would a strict gate
# fail" signal; a strict advisory pass (CCN>10, length>60, params>5) surfaces refactor
# candidates. The `*global*` pseudo-function Lizard emits for file scope is filtered out
# of the candidate list as noise.
run_complexity() {
  hdr "Complexity — Lizard (C# + TypeScript)"
  local LZ
  if ! LZ="$(lizard_cmd)"; then
    skip "lizard — install with 'pipx install lizard' (or 'pip install lizard')"
    return 0
  fi

  local found=0
  for spec in "csharp:$BACKEND_DIR" "typescript:$FRONTEND_SRC"; do
    local lang="${spec%%:*}" path="${spec#*:}"
    [[ -e "$path" ]] || continue
    echo "-- $lang ($path)"

    # CI-equivalent gate view (default thresholds) — these are the ones that matter most.
    local warns
    warns="$($LZ "$path" --languages "$lang" -x "*/obj/*" -x "*/bin/*" -w 2>/dev/null || true)"
    if [[ -n "$warns" ]]; then
      echo "$warns"
      found=1
    else
      echo "   no functions over default thresholds (CCN>15)"
    fi

    # Refactor candidates (strict), file-scope *global* noise removed.
    local cand
    cand="$($LZ "$path" --languages "$lang" -x "*/obj/*" -x "*/bin/*" -C 10 -L 60 -a 5 -w 2>/dev/null \
      | grep -v '\*global\*' || true)"
    local n; n="$(printf '%s' "$cand" | grep -c 'warning:' || true)"
    [[ "$n" -gt 0 ]] && echo "   ($n refactor candidate(s) at strict thresholds CCN>10 — see full CSV below)"

    # Machine-readable CSV for CI artifacts.
    $LZ "$path" --languages "$lang" -x "*/obj/*" -x "*/bin/*" --csv \
      > "$OUT_DIR/lizard-$lang.csv" 2>/dev/null || true
  done

  [[ "$found" -eq 1 ]] && FINDINGS=$((FINDINGS + 1))
  echo "   CSV: $OUT_DIR/lizard-*.csv"
}

# --- Static analysis: JetBrains InspectCode over the .NET solution -----------------
run_inspect() {
  hdr "Static analysis — InspectCode (.NET)"
  if ! command -v jb >/dev/null 2>&1; then
    skip "jb — install with 'dotnet tool install -g JetBrains.ReSharper.GlobalTools'"
    return 0
  fi
  [[ -f "$SOLUTION" ]] || { skip "solution not found: $SOLUTION"; return 0; }

  local sarif="$OUT_DIR/inspectcode.sarif"
  # CI already builds the solution (backend-ci), so it can set INSPECT_NO_BUILD=1 to skip
  # the rebuild. Locally we build (reliable) rather than assume a fresh binary.
  local nobuild=(); [[ -n "${INSPECT_NO_BUILD:-}" ]] && nobuild=(--no-build)
  echo "-- analysing $SOLUTION (WARNING+); this builds the solution and can take a few minutes"
  jb inspectcode "$SOLUTION" --output="$sarif" --format=Sarif --severity=WARNING "${nobuild[@]}" >/dev/null 2>&1 || true

  if [[ -f "$sarif" ]]; then
    # Count SARIF results without a JSON dependency: one "ruleId" per result.
    local n; n="$(grep -c '"ruleId"' "$sarif" 2>/dev/null || echo 0)"
    if [[ "$n" -gt 0 ]]; then
      echo "   $n issue(s) at WARNING+ — SARIF: $sarif"
      FINDINGS=$((FINDINGS + 1))
    else
      echo "   no issues at WARNING+ — SARIF: $sarif"
    fi
  else
    echo "   (no SARIF produced — InspectCode may not support this solution format; see the skill for the fallback)"
  fi
}

# --- Static analysis: actionlint over the GitHub Actions workflows -----------------
run_actions() {
  hdr "Static analysis — actionlint (GitHub Actions)"
  if ! command -v actionlint >/dev/null 2>&1; then
    skip "actionlint — install with 'brew install actionlint' (or see https://github.com/rhysd/actionlint)"
    return 0
  fi
  [[ -d .github/workflows ]] || { echo "   no .github/workflows to lint"; return 0; }

  local out; out="$(actionlint 2>&1 || true)"
  if [[ -n "$out" ]]; then
    echo "$out"
    FINDINGS=$((FINDINGS + 1))
  else
    echo "   no workflow issues"
  fi
}

case "$TARGET" in
  complexity) run_complexity ;;
  inspect)    run_inspect ;;
  actions)    run_actions ;;
  all)        run_complexity; run_inspect; run_actions ;;
  *) echo "Usage: scripts/static-analysis.sh [all|complexity|inspect|actions] [--strict]"; exit 2 ;;
esac

echo
echo "------------------------------------------------------------"
echo "Static analysis (advisory): $FINDINGS surface(s) with findings, $SKIPPED tool(s) skipped."
[[ "$SKIPPED" -gt 0 ]] && echo "Install the skipped tools for full coverage (see hints above)."
if [[ "$STRICT" -eq 1 && "$FINDINGS" -gt 0 ]]; then
  echo "Strict mode: failing because findings were reported."
  exit 1
fi
exit 0
