#!/usr/bin/env bash
# Publish a model-graded evaluation run to the Foundry portal (BYO-responses), in two steps:
#   1. Ask the live copilot each dataset query and write {id,query,response} JSONL (GA path).
#   2. Publish an evaluation run over those answers via the OpenAI Evals API against Foundry
#      /openai/v1 (isolated preview-free tool). Results appear in the Foundry portal → Evaluation.
#
# Prereqs: provisioned Foundry (`azd provision`) + Azure sign-in. Off the blocking gate.
#
#   scripts/portal-eval.sh [answers.jsonl]
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if command -v azd >/dev/null 2>&1; then
  # shellcheck disable=SC2046
  export $(azd env get-values 2>/dev/null | grep -E "AZURE_FOUNDRY" | xargs -r)
fi

if [[ -z "${AZURE_FOUNDRY_PROJECT_ENDPOINT:-}" ]]; then
  echo "AZURE_FOUNDRY_PROJECT_ENDPOINT is not set — provision Foundry (azd provision) and sign in first." >&2
  exit 2
fi

answers="${1:-$(python3 -c 'import tempfile,os;print(os.path.join(tempfile.gettempdir(),"homescout-answers.jsonl"))')}"

echo "Step 1/2 — generating BYO answers from the live copilot → $answers"
dotnet run --project dotnet/tools/HomeScoutCopilot.Evaluator -c Release -- answers --out "$answers"

echo "Step 2/2 — publishing the evaluation run to the Foundry portal"
dotnet run --project dotnet/tools/HomeScoutCopilot.PortalEval -c Release -- --data "$answers"
