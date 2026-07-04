#!/usr/bin/env bash
# Run HomeScout's model-graded answer-quality evaluation (live) and generate the aieval report.
#
# Asks the live copilot each dataset query, then scores every real answer with Microsoft's built-in
# quality evaluators, HomeScout's bespoke judge, and the deterministic guardrails — plus the Foundry
# content-harm evaluators when AZURE_EVAL_CONTENT_SAFETY=1. Results persist to the eval store (local
# by default; Azure ADLS Gen2 when AZURE_EVAL_STORAGE_ENDPOINT is set) keyed by execution name for
# regression history, and a shareable HTML report is written to artifacts/eval-report.html.
#
# Prereqs: provisioned Foundry (`azd provision`) and Azure sign-in. Off the blocking gate (External).
#
#   scripts/eval-report.sh
#   AZURE_EVAL_CONTENT_SAFETY=1 scripts/eval-report.sh   # also run Foundry content-safety
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# Pull the Foundry + eval-store settings from the azd environment.
if command -v azd >/dev/null 2>&1; then
  # shellcheck disable=SC2046
  export $(azd env get-values 2>/dev/null | grep -E "AZURE_FOUNDRY|AZURE_EVAL_STORAGE" | xargs -r)
fi

if [[ -z "${AZURE_FOUNDRY_PROJECT_ENDPOINT:-}" ]]; then
  echo "AZURE_FOUNDRY_PROJECT_ENDPOINT is not set — provision Foundry (azd provision) and sign in first." >&2
  exit 2
fi

local_store="${AZURE_EVAL_STORAGE_PATH:-$(python3 -c 'import tempfile,os;print(os.path.join(tempfile.gettempdir(),"homescout-eval-store"))')}"
export AZURE_EVAL_STORAGE_PATH="$local_store"

echo "Running the live answer-quality evaluation (this makes real model calls)…"
dotnet test dotnet/tests/HomeScoutCopilot.Evaluation.Test/HomeScoutCopilot.Evaluation.Test.csproj \
  -c Release --filter "TestCategory=External"

report_source="${AZURE_EVAL_STORAGE_ENDPOINT:-$local_store}"
mkdir -p artifacts
echo "Generating report from ${report_source}…"
( cd dotnet && dotnet tool run aieval report --path "$local_store" --output ../artifacts/eval-report.html )
echo "Report: artifacts/eval-report.html"
