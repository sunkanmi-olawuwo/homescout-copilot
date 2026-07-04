# GenAIOps Tooling Plan

*AgentOps (deploy) + Evaluator (evaluation) — the two .NET tool projects.*

**Decision: yes — two dedicated .NET console tool projects, not one.** A control-plane tool
for **deploying/managing** Foundry content (agent versions, indexes, datasets, connections)
and a separate tool for **evaluation**. They have different lifecycles, different invocation
contexts, and different blast radius, so single-responsibility wins. Both are pure .NET
(confirmed feasible — see [[GenAIOps Reference Implementation]]) and keyless via
`DefaultAzureCredential`.

This is the concrete home for the ".NET deploy step" and ".NET eval harness" the
[Phased Learning And Build Plan](../00-roadmap/phased-learning-build-plan.md) Phase 3/6
calls for.

**Status (2026-07-04):** `HomeScoutCopilot.AgentOps` exists with the **manifest step
implemented** — `agentops manifest [--out <path>]` assembles the declarative
`homescout.agent.yaml` from the single-sourced agent definition (`AgentPrompt` +
`HomeScoutAgentTools.ToolNames`), committed at
`dotnet/src/HomeScoutCopilot.API.Service/Prompts/homescout.agent.yaml` and drift-guarded by a
test. The live `AgentAdministrationClient.CreateAgentVersion` registration is queued next
(needs `azd` provision — now available). **`HomeScoutCopilot.Evaluator` exists** with:
- `evaluator safety [--data <path>]` — deterministic HomeScout guardrail evaluators
  (not-mortgage-advice, no product recommendation, no safe/unsafe area verdict) over a
  version-controlled eval dataset (`data/homescout-eval.jsonl`, drift-guarded), scoring pass
  rates + failures (exit 1 on any). Offline.
- `evaluator run [--data <path>]` — asks the **live** copilot each dataset *query* and runs the
  same safety evaluators over the **real** answers. Needs `AZURE_FOUNDRY_*` + Azure creds;
  proven by `EvaluatorLiveTests` (`[Category("External")]`, off the blocking gate — verified
  6/6 pass against the provisioned agent, 2026-07-04).
- `evaluator quality [--data <path>]` — **bespoke model-graded quality** (done, live-verified):
  asks the live copilot each query, then an **LLM judge** scores each real answer on relevance /
  usefulness / groundedness (1–5, pass ≥ 3) with a rationale, and reports averages + pass rate.
  The judge is a tool-less agent on the same proven `AsAIAgent` path (`FoundryAnswerJudge`);
  the rubric + score-parsing (`AnswerJudge`) are pure and offline-tested; `QualityLiveTests`
  (`[Category("External")]`) verifies it live. This is the lightweight, gate-friendly bespoke
  signal — it needs no eval store.

### Standard-library evaluation harness (`HomeScoutCopilot.Evaluation.Test`)

Alongside the bespoke console verb, HomeScout runs the **first-party
`Microsoft.Extensions.AI.Evaluation` libraries** — the .NET equivalent of what `mslearn-genaiops`
does in Python — so quality is measured with Microsoft's research-validated evaluators, not only
our own rubric. **We deliberately run both, each metric labelled by origin**, in one
`ReportingConfiguration` so the report compares them side-by-side (done, live-verified 2026-07-04):

- **Built-in quality** (`Evaluation.Quality`, LLM-graded): `RelevanceEvaluator`,
  `CoherenceEvaluator`, `FluencyEvaluator`. Judge = the Foundry `chat` deployment via an
  `IChatClient`.
- **HomeScout bespoke judge** (`HomeScoutBespokeJudgeEvaluator`) — the same `AnswerJudge` rubric,
  reused as a custom `IEvaluator`, labelled `HomeScout bespoke: …`.
- **HomeScout guardrails** (`HomeScoutGuardrailEvaluator`) — the deterministic `SafetyEvaluators`
  regexes as a custom `IEvaluator`, so a violation reads the same in the report as in the CI gate.
  Guardrail failures are the one **hard** assertion; quality is tracked as a trend.
- **Foundry content-safety** (`Evaluation.Safety`, opt-in `AZURE_EVAL_CONTENT_SAFETY=1`):
  `HateAndUnfairness` / `Violence` / `SelfHarm` / `Sexual` via the Foundry safety service.
  Complements — does not replace — our domain guardrails (there is no built-in evaluator for
  "not mortgage advice"). Verified 0–1 severity (all safe) across the dataset.

Judge-model note: the `chat` deployment is a **gpt-5 reasoning model** — it rejects
`temperature=0`, so a `DefaultTemperatureChatClient` shim strips it (the built-in evaluators
hard-code temperature 0). The strict built-in evaluators also occasionally fail to parse a
reasoning-model response; that (and throttling) is treated as **non-blocking** variance, while a
400 / auth error **blocks** — so the harness catches a real integration break (it caught the
temperature bug) without going red on LLM non-determinism.

**Cloud store + regression history + shareable reports.** Results and cached judge responses
persist to an **Azure ADLS Gen2** store (`AzureStorageReportingConfiguration`) when
`AZURE_EVAL_STORAGE_ENDPOINT` is set — keyless (Entra RBAC), keyed by execution name so scores
line up across runs; disk is the local default. `scripts/eval-report.sh` runs the evaluation and
writes `artifacts/eval-report.html` via the `dotnet aieval` tool (pinned in `dotnet-tools.json`).
The storage account is provisioned by `infra/modules/eval-storage.bicep` (verified live 2026-07-04).

The Foundry Evals *service* (upload dataset → `builtin` evaluators → **portal** runs via
`Azure.AI.Projects`) remains an optional future path if we want portal-visible evaluation runs;
the standard library already delivers the model-graded quality signal + cloud regression history.

## Why two projects (not one, not in the test project)

| Concern | AgentOps (deploy/control-plane) | Evaluator (measurement) |
| --- | --- | --- |
| Job | Register versioned agents; manage indexes, datasets, connections | Run eval sets, score, report |
| When | Release time / manual, infrequent, **mutating** | CI (scheduled/opt-in) + local, **read-mostly** |
| Blast radius | Changes live Foundry resources | Produces scores + a report |
| Failure impact | A bad deploy affects prod | A failed eval blocks nothing (non-gate) |

Keeping them separate means CI can run the Evaluator without linking in deploy/mutation
capability, and a release step can deploy without pulling eval dependencies. (A test project
is the wrong home: these are operational tools invoked as programs, not test suites — though
their *pure* logic is unit-tested in the fast gate.)

## Structure (RagLab-consistent — a new `tools/` alongside `src/` and `tests/`)

```
dotnet/
  src/     … (product runtime — unchanged)
  tools/   (NEW — operational tools; not shipped in the API runtime)
    HomeScoutCopilot.AgentOps/         # "DeployTool": deploy/manage Foundry content
      Program.cs                        #   verbs: deploy-agent, list-agents, (later) index, dataset
    HomeScoutCopilot.Evaluator/         # run evaluations, score, report
      Program.cs                        #   verbs: run-eval, upload-dataset, report
  tests/   … (+ AgentOps.Test / Evaluator.Test for pure-logic unit tests)
```

Add a `/tools/` solution folder in `HomeScoutCopilot.slnx`. Both tool projects and their
unit tests build with the solution, so `backend-ci` compiles them (compile-safety in the
gate) without running any live Azure calls.

## Shared foundation (single-source the agent definition)

Both tools **reference `HomeScoutCopilot.API.Service`** (and `.Shared`) to reuse the agent
definition already there — `AgentPrompt` (the versioned prompt), `HomeScoutAgentTools` (the
tool set), `FoundryOptions`, and the `AIProjectClient` wiring — so the agent is defined
**once** and both runtime and deploy use the same source. If the coupling to
`.API.Service` (which references `Microsoft.AspNetCore.App`) becomes awkward, extract a small
`HomeScoutCopilot.Agent` library holding just the Foundry client factory + agent definition,
referenced by `.API.Service` and both tools. Not needed yet — start by referencing
`.API.Service`.

## HomeScoutCopilot.AgentOps ("the DeployTool")

Responsibilities (grows by phase):

- **Phase 3 — agents:** register the versioned agent from the prompt asset via
  `AIProjectClient.AgentAdministrationClient.CreateAgentVersion(...)` (or
  `CreateAgentFromManifest(...)` for the declarative `homescout.agent.yaml` manifest). Prints
  the new server-side version. Git-tag the deploy so *tag ↔ prompt version ↔ agent version*
  line up.
- **Phase 6 — indexes + datasets:** create/update AI Search indexes for case-file / curated
  knowledge retrieval; upload datasets via `AIProjectClient.Datasets`.
- **Later — connections:** manage project connections as needed.

Invocation: `dotnet run --project dotnet/tools/HomeScoutCopilot.AgentOps -- deploy-agent`
(reads `FoundryOptions` from config/env — the same `AZURE_FOUNDRY_*` azd outputs the API
uses). Keyless `DefaultAzureCredential`. Run at release time / manually; **not** in the
blocking PR gate.

## HomeScoutCopilot.Evaluator

Responsibilities:

- Upload a versioned **eval dataset** (`query` / `response` / `ground_truth`) —
  `AIProjectClient.Datasets`.
- Create + run an evaluation — `AIProjectClient.GetProjectOpenAIClient().GetEvaluationClient()`
  (`OpenAI.Evals`) or the Foundry-native `AIProjectClient.Evaluators`.
- Evaluators: built-in **intent resolution / relevance / groundedness**, **plus HomeScout
  safety evaluators** — "avoids regulated mortgage advice", "no simplistic safe/unsafe area
  label", "separates facts / estimates / assumptions / missing data".
- Poll, score (1–5, pass ≥ 3), write a results summary + `report_url` for a CI comment.

Invocation: `dotnet run --project dotnet/tools/HomeScoutCopilot.Evaluator -- run-eval`. Runs
locally and in a **non-blocking** scheduled/opt-in workflow (like `external-checks`) with
**Azure OIDC** login — a third-party/model outage must never block a merge (per the
"verify, don't assume" standard in `AGENTS.md`).

## Testing

- **Fast gate (unit):** pure logic only — arg/verb parsing, manifest building, dataset
  serialization, result formatting — in `AgentOps.Test` / `Evaluator.Test`. No Azure.
- **External (nightly/opt-in):** the real deploy + real eval as `[Category("External")]`
  runs where Foundry + Azure creds exist; excluded from the blocking gate.
- Datasets, eval definitions, and the agent manifest are **version-controlled assets**
  (GenAIOps): they live in the repo and change through PRs.

## Acceptance criteria

- `dotnet/tools/HomeScoutCopilot.AgentOps` registers a versioned Foundry agent from the
  prompt asset/manifest; prints the version; verified by an `[Category("External")]` run.
- `dotnet/tools/HomeScoutCopilot.Evaluator` runs the HomeScout eval set (built-in + safety
  evaluators) and emits a scored summary; verified live off the blocking gate.
- Both build with the solution; pure-logic unit tests pass in the fast gate; drift 0 fail.
- No Python in the repo (total .NET stack — [[Plan Divergence]]).

## Phase mapping

| Tool capability | Owning phase |
| --- | --- |
| AgentOps: deploy versioned agent (+ manifest) | 3 |
| Evaluator: first hand-curated eval set + safety evaluators | 3 |
| Evaluator: CI eval gate (OIDC, non-blocking) | 6 |
| AgentOps: indexes + datasets for RAG | 6 |
| AgentOps: connections, broader ops | 7 |
