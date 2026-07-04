# GenAIOps Reference Implementation

Microsoft's official GenAIOps lab repo (`MicrosoftLearning/mslearn-genaiops`) is the **reference implementation** for the
"Operationalize generative AI applications" learning path. It is the concrete,
end-to-end shape of everything [[GenAIOps Learning Path Integration]] describes in the
abstract: versioned agents, prompt management, cloud evaluation, batch experiments, and
CI-gated evals. This page catalogs the patterns worth adopting and maps each to its
HomeScout (.NET / Microsoft Agent Framework) translation and the owning phase in the
[Phased Learning And Build Plan](../00-roadmap/phased-learning-build-plan.md).

**Source:** https://github.com/MicrosoftLearning/mslearn-genaiops (default branch `main`).
This is a **reference only** — HomeScout is .NET/API-first with a "not mortgage advice"
product boundary; the lab is a Python single-agent trail-guide sample. Adopt the *shape*
and discipline, not the code. Do not vendor it. Re-check it during
[[Release Monitoring]].

> Auth confirmation: the lab authenticates keyless with `DefaultAzureCredential`
> everywhere — the same choice HomeScout already ships (`FoundryAgentGateway`,
> [Program.cs](../../../dotnet/src/HomeScoutCopilot.API/Program.cs)). No API keys.

## Reference repo shape

```
mslearn-genaiops/
├─ azure.yaml                              # azd app definition
├─ infra/                                  # bicep: AI project, connections, ACR,
│  ├─ main.bicep                           #   App Insights, Log Analytics, AI Search,
│  └─ core/{ai,host,monitor,search,storage}/  #   Bing grounding, storage
├─ data/…evaluation_dataset.jsonl          # eval dataset (query/response/ground_truth)
├─ src/
│  ├─ agents/
│  │  ├─ trail_guide_agent/
│  │  │  ├─ trail_guide_agent.py           # DEPLOY script → agents.create_version(...)
│  │  │  ├─ agent.yaml                      # declarative agent manifest
│  │  │  └─ prompts/v1_instructions.txt …  # versioned prompt files (v1..v4_optimized_concise)
│  │  └─ monitoring_agent/*-prompt.py       # prompt variants (start/short/error/solution)
│  ├─ evaluators/evaluate_agent.py          # cloud eval pipeline (Foundry Evals API)
│  └─ tests/
│     ├─ run_batch_tests.py                 # batch experiment harness (+ token capture)
│     ├─ test-prompts/*.txt                 # experiment input prompts
│     ├─ check_traces.py / run_monitoring.py
│     └─ interact_with_agent.py
├─ evaluation_results.txt                   # eval summary, committed for the CI PR comment
└─ .github/workflows/evaluate-agent.yml     # CI: run eval → comment on PR (OIDC login)
```

## Patterns to adopt

### 1. Persisted, *versioned* agents — deploy step, then reference by name

The lab's [`trail_guide_agent.py`](https://github.com/MicrosoftLearning/mslearn-genaiops/blob/main/src/agents/trail_guide_agent/trail_guide_agent.py)
reads the prompt from a file and **registers a server-side agent with an
auto-incrementing version**:

```python
agent = project_client.agents.create_version(
    agent_name=os.environ["AGENT_NAME"],
    definition=PromptAgentDefinition(model=..., instructions=instructions),
)
# → agent.id, agent.version   (visible + rollback-able in the Foundry portal)
```

Runtime then **references the agent by name** (Responses API `agent_reference`), never
redefining it inline.

- **HomeScout today:** `FoundryAgentGateway` builds an **in-process** agent per request
  via `AIProjectClient.AsAIAgent(...)` — nothing is persisted server-side, so our only
  versioning is the git history of the prompt asset (see
  [Copilot Agent Gateway](../03-backend/copilot-agent-gateway-plan.md)).
- **Translation:** introduce a **deploy step** that registers the prompt as a versioned
  Foundry agent, and have the gateway reference it by name. This decouples prompt
  changes from app deploys and gives portal-visible versions + rollback.
- **Resolved (2026-07-04 spike): .NET exposes this directly.** No Python needed —
  `Azure.AI.Projects.AIProjectClient.AgentAdministrationClient` (from
  `Azure.AI.Projects.Agents` 2.0.0, already on our restore graph via
  `Microsoft.Agents.AI.Foundry` 1.5.0) offers `CreateAgentVersion(Async)`,
  `CreateAgentFromManifest(Async)` / `CreateAgentVersionFromManifest(Async)`,
  `GetAgentVersion(s)`, and `DeleteAgentVersion`. Definition types:
  `ProjectsAgentDefinition`, `ProjectsAgentVersionCreationOptions`,
  `DeclarativeAgentDefinition` (== `PromptAgentDefinition` / declarative `agent.yaml`).
  `ProjectsAgentVersion.Version` gives the server-side version. Runtime references it by
  name/version via `Azure.AI.Extensions.OpenAI.AgentReference` (has `.Version`) over the
  Responses API. See the verified-surface table below.
- **Owning phase:** 3 (prompt governance / first agent).

### 2. Declarative agent manifest (`agent.yaml`)

[`agent.yaml`](https://github.com/MicrosoftLearning/mslearn-genaiops/blob/main/src/agents/trail_guide_agent/agent.yaml)
declares the agent in one file: `name`, `model`, `instructions_file`. Model + name +
which prompt version live together, declaratively, instead of scattered across code and
options.

- **Translation:** a `homescout.agent.yaml` next to the prompt asset
  (`name: homescout`, `model: <deployment>`, `instructions_file: Prompts/homescout.v1.md`)
  as the single source of truth the deploy step reads and `FoundryOptions` aligns to.
- **Owning phase:** 3 (cheap, reversible; can land alongside the current prompt asset).

### 3. Versioned prompt files + variants

Prompts are plain files versioned by filename (`v1_instructions.txt` …
`v4_optimized_concise.txt`), with variant sets for experiments (the `monitoring_agent`
keeps `start/short/error/solution` prompt variants).

- **HomeScout today:** ✅ already done — `Prompts/homescout.v1.md` embedded + loaded by
  `AgentPrompt` (PR #27). Evolve by adding `homescout.vN.md` + bumping `AgentPrompt.Version`.
- **Add:** keep experiment variants side-by-side when we start optimizing, and **git-tag
  the deploy** that ships each version so *tag ↔ prompt version ↔ agent version* line up.
- **Owning phase:** 3–4.

### 4. Cloud evaluation harness (the biggest current gap)

[`evaluate_agent.py`](https://github.com/MicrosoftLearning/mslearn-genaiops/blob/main/src/evaluators/evaluate_agent.py)
is a full pipeline against Foundry's OpenAI-compatible **Evals API**:

1. Upload a **JSONL dataset** (`query` / `response` / `ground_truth`) via
   `project_client.datasets.upload_file`.
2. `client.evals.create(...)` with **built-in, model-judged evaluators** —
   `builtin.intent_resolution`, `builtin.relevance`, `builtin.groundedness` — and a
   `{{item.<col>}}` data mapping.
3. `client.evals.runs.create(...)`, poll to completion.
4. Score on a **1–5 scale, pass ≥ 3**; write `evaluation_results.txt` (avg + pass rate)
   and emit `report_url` as a GitHub Actions output.

- **HomeScout today:** we test the deterministic tools and the guardrail *text*, but do
  **not** score answer quality. This is exactly the artifact
  [[GenAIOps Learning Path Integration]] modules 3–4 call for.
- **Translation:** a HomeScout eval set of realistic homebuying scenarios (two homes with
  commute/school tradeoffs; a cheaper home with higher running-cost risk; a listing with
  missing facts) scored for **groundedness, usefulness/relevance, intent resolution**,
  **plus HomeScout-specific safety evaluators**: "avoids regulated mortgage advice", "no
  simplistic safe/unsafe area label", "separates facts / estimates / assumptions /
  missing data". Runs as `[Category("External")]` (off the blocking gate).
- **Owning phase:** 3 (first hand-curated set) → 6 (automate once stable).

### 5. Batch experiment harness with token/cost capture

[`run_batch_tests.py`](https://github.com/MicrosoftLearning/mslearn-genaiops/blob/main/src/tests/run_batch_tests.py)
runs a folder of `test-prompts/*.txt` against the deployed agent (fresh conversation per
prompt via the Responses API `agent_reference`), captures **response + token usage**, and
writes `experiments/{name}/agent-responses.json` for A/B comparison across prompt versions.

- **Translation:** a small HomeScout experiment runner over a `test-prompts/` set that
  records tokens + latency per answer — feeds the cost-awareness metrics that
  [[GenAIOps Learning Path Integration]] module 5 wants (per-comparison token/cost).
- **Owning phase:** 4 (compare ≥2 prompt variants) → 5/6.

### 6. CI eval gate (evals as a PR comment, OIDC login)

[`evaluate-agent.yml`](https://github.com/MicrosoftLearning/mslearn-genaiops/blob/main/.github/workflows/evaluate-agent.yml):
runs the eval on PRs touching the agent, **comments the results on the PR**, and fails on
an error marker. Notable:

- **Keyless CI auth via OIDC** — `azure/login@v2` with `client-id` / `tenant-id` /
  `subscription-id` and `permissions: id-token: write` (no stored secret credential).
- Least-privilege `permissions: { contents: read, pull-requests: write, id-token: write }`.

- **Translation:** an `external-checks`-style scheduled/opt-in workflow that runs the
  HomeScout eval with **Azure OIDC** (the same keyless CI auth we flagged for the live
  Foundry test), posts a summary, and stays **off the blocking PR gate** (a third-party
  outage must not block merges — consistent with the "verify, don't assume" standard in
  `AGENTS.md`).
- **Owning phase:** 6 (automate evals) — pairs with 7 (deployment/monitoring).

### 7. Reproducible infra via azd + bicep

`azure.yaml` + `infra/main.bicep` + `infra/core/**` provision the AI project, model,
connections, App Insights + Log Analytics (monitoring), AI Search, and storage with `azd`.

- **HomeScout today:** ✅ we have `infra/` (azd + bicep, **Basic** Foundry setup). The lab
  shows the **fuller** shape we grow into: App Insights / Log Analytics (module 5
  monitoring), AI Search + storage (Standard agents / RAG, Phase 6+).
- **Owning phase:** 7 (Azure deployment management), incrementally.

## Cross-cutting decision: language of the eval/deploy tooling — RESOLVED: total .NET

**Decision (2026-07-04, from the spike): keep a single .NET stack — no Python in the
repo.** The lab's Python scripts are **guided examples only**; every step maps to a .NET
API that is already on our restore graph. The eval/deploy tooling ships as two dedicated
.NET console tools under `dotnet/tools/` — **`HomeScoutCopilot.AgentOps`** (deploy/manage
agents, indexes, datasets) and **`HomeScoutCopilot.Evaluator`** (run evaluations) — see
[[GenAIOps Tooling Plan]]. Both authenticate keyless with `DefaultAzureCredential` like the
rest of the app.

### Verified .NET surface (spike over the restored assemblies)

Reflection over `Azure.AI.Projects` 2.0.0, `Azure.AI.Projects.Agents` 2.0.0, `OpenAI`
2.10.0, and `Azure.AI.Extensions.OpenAI` 2.0.0 (all pulled in transitively today):

| Python (lab) | .NET equivalent (verified present) |
| --- | --- |
| `AIProjectClient(endpoint, DefaultAzureCredential())` | `Azure.AI.Projects.AIProjectClient` (+ `Azure.Identity`) — already used |
| `project_client.agents.create_version(name, PromptAgentDefinition(...))` | `AIProjectClient.AgentAdministrationClient.CreateAgentVersion(...)` with `ProjectsAgentVersionCreationOptions` / `ProjectsAgentDefinition` |
| declarative `agent.yaml` | `AgentAdministrationClient.CreateAgentFromManifest(...)` + `DeclarativeAgentDefinition` (native) |
| `project_client.datasets.upload_file(...)` | `AIProjectClient.Datasets` (+ `FileDataset`, `PendingUploadResult`) |
| `project_client.get_openai_client()` | `AIProjectClient.GetProjectOpenAIClient()` / `.ProjectOpenAIClient` |
| `client.evals.create(...)` / `runs.create` / `output_items.list` | `OpenAIClient.GetEvaluationClient()` → `OpenAI.Evals.EvaluationClient.CreateEvaluation / CreateEvaluationRun / GetEvaluationRunOutputItems` |
| built-in evaluators (intent/relevance/groundedness) | Foundry-native `AIProjectClient.Evaluators` (`Azure.AI.Projects.Evaluation.*`, `PromptBasedEvaluatorDefinition`, `EvaluatorType`) **or** `OpenAI.Graders.*` |
| runtime `agent_reference` | `Azure.AI.Extensions.OpenAI.AgentReference` (has `.Version`) + `AsAgentResponseItem` |

Caveat: this is the `Azure.AI.Projects*` 2.0.0 surface — confirm any preview/GA labels and
exact call shapes against Microsoft Learn at implementation time (per the "use the current,
documented, non-deprecated API surface" standard). Feasibility is proven; the details get
pinned when we build.

## Summary: what to infuse where

| Pattern | HomeScout status | Owning phase |
| --- | --- | --- |
| Keyless `DefaultAzureCredential` | ✅ shipped | done |
| Versioned prompt files | ✅ shipped (`homescout.v1.md`) | done |
| Declarative `agent.yaml` manifest | to add | 3 |
| Persisted/versioned agent (`create_version` + reference-by-name) | to add — **.NET path confirmed** (`AgentAdministrationClient.CreateAgentVersion`) | 3 |
| Cloud eval harness (built-in + safety evaluators) | **gap** — top priority; **.NET path confirmed** (`OpenAI.Evals` / `AIProjectClient.Evaluators`) | 3 → 6 |
| Eval/deploy tooling language | **decided: total .NET** (no Python) | — |
| Batch experiment harness (+ token/cost) | to add | 4 |
| CI eval gate (OIDC, PR comment, non-blocking) | to add | 6 |
| Fuller azd/bicep (App Insights, Search, storage) | partial (Basic) | 7 |
