# Plan Divergence

Use this page whenever implementation differs from:

- the course companion repo
- a playlist step
- an existing product plan
- an existing wiki page

## Entries

### 2026-07-04: End-User Auth Uses Keycloak, Not Entra ID

Course/plan expectation:

- The course (video 09, [[Part 9: User Auth]]) adds user authentication with **Microsoft Entra
  ID**, and `CLAUDE.md` calls for **Microsoft Entra identity**.

HomeScout direction (decided):

- **End-user / application sign-in will use Keycloak** (OIDC), following the same choice made in the
  **RagLab** project — self-hosted, portable, no Azure CIAM dependency.

Important scope distinction:

- This is about **end-user login / per-user data** only. **Azure resource access is unchanged** and
  still uses **Entra managed identity / `DefaultAzureCredential`** (keyless Foundry + storage RBAC) —
  Azure RBAC has no Keycloak path, and CLAUDE.md's "managed identity, least-privilege" refers to
  *resource* access, which stays Entra.

Impact:

- When per-user features land, wire Keycloak OIDC → stable user id. Until then, multi-turn threads
  can run **anonymous** (session-scoped), so this decision doesn't block conversation work.

### 2026-07-04: Model-Graded Quality — Both The Standard Library And A Bespoke Judge (Not The Portal Evals Service)

Plan/reference expectation:

- `mslearn-genaiops` measures answer quality with the Azure AI Evaluation SDK / **Foundry Evals
  service** (Python) — built-in `relevance` / `groundedness` / `coherence` evaluators, optionally
  run in the cloud with results in the Foundry portal.

HomeScout direction (implemented):

- We adopted the **first-party .NET `Microsoft.Extensions.AI.Evaluation` libraries** — the direct
  equivalent — running the built-in `Relevance` / `Coherence` / `Fluency` (and opt-in
  content-safety) evaluators, with **cloud ADLS Gen2 persistence** for regression history +
  `dotnet aieval` reports (`HomeScoutCopilot.Evaluation.Test`).
- We **also keep our bespoke LLM-judge** (`AnswerJudge`), both as the lightweight `evaluator
  quality` console verb and as a custom `IEvaluator` inside the same report — deliberately
  side-by-side with the built-in metrics, each labelled by origin.

Reasons:

- The bespoke judge was built first because it reused a **proven** SDK path (`AsAIAgent`), so we
  had a live-verified quality signal immediately. On review we found the first-party evaluation
  library is the sanctioned, research-validated equivalent and is pure .NET, so we adopted it
  rather than reinvent — keeping the bespoke rubric as an explicit comparison (the user asked for
  both). All live-verified 2026-07-04 against the provisioned Foundry.

Remaining divergence:

- We use the library's **local + Azure-storage** reporting, not the Foundry **portal** evaluation
  runs (`Azure.AI.Projects` `EvaluationClient`). The portal runs stay an **optional** later path;
  the ADLS store already gives cloud regression history + shareable reports.

### 2026-07-04: GenAIOps Tooling — Total .NET Stack (Python Samples Are Guidance Only)

Context:

- Microsoft's GenAIOps lab (`mslearn-genaiops`, see [[GenAIOps Reference Implementation]])
  implements agent versioning + evaluation in **Python**. Question was whether HomeScout
  needs Python for the deploy/eval tooling or can stay pure .NET.

Decision:

- **Total .NET stack — no Python in the repo.** A reflection spike over the restored
  assemblies confirmed the full pipeline exists in .NET:
  - Persisted, versioned agents: `Azure.AI.Projects.AIProjectClient.AgentAdministrationClient`
    → `CreateAgentVersion` / `CreateAgentFromManifest` + `ProjectsAgentDefinition` /
    `DeclarativeAgentDefinition` (from `Azure.AI.Projects.Agents` 2.0.0, already transitive
    via `Microsoft.Agents.AI.Foundry` 1.5.0).
  - Evaluation: `AIProjectClient.GetProjectOpenAIClient().GetEvaluationClient()`
    (`OpenAI.Evals.EvaluationClient`) and/or Foundry-native `AIProjectClient.Evaluators` /
    `.Datasets` (`FileDataset`, `PendingUploadResult`).
  - Runtime reference-by-name: `Azure.AI.Extensions.OpenAI.AgentReference`.

Reason:

- Stack unity, one CI toolchain, keyless `DefaultAzureCredential` throughout. The .NET
  surface is present today; Python examples remain reference guides only.

Impact:

- Phase 3 moves to a **.NET** agent-deploy step (register versioned agent) + **.NET** eval
  harness. Confirm any preview/GA labels + exact call shapes against Microsoft Learn at
  implementation time (per the non-deprecated-API-surface standard).

### 2026-07-04: `dotnet/tools/` Projects Added On Real Need (AgentOps + Evaluator)

Master-plan rule:

- "No premature scaffolding" — `infra/`, `poc/`, tool scaffolding are added only when a real
  need arrives, not mirrored as empty folders up front.

Direction:

- A new `dotnet/tools/` solution folder holds two operational console tools —
  `HomeScoutCopilot.AgentOps` (deploy/manage agents, indexes, datasets) and
  `HomeScoutCopilot.Evaluator` (run evaluations). See [[GenAIOps Tooling Plan]].

Reason / consistency:

- This **honours** the rule rather than breaking it: the persisted-agent-deploy + eval work
  (Phase 3) is the real need. Same reasoning that justified adding `infra/` early for the
  Foundry slice. The tools are created **when that work starts**, not now as empty stubs.

Impact:

- The `src`/`tests` layout gains a sibling `tools/`. Tools reference `.API.Service` (reuse
  the single-sourced agent definition) and are excluded from the API runtime; they build
  with the solution so `backend-ci` gives them compile-safety.

### 2026-07-04: Agent Prompt Externalised To A Versioned Embedded Asset (GenAIOps)

Previous state:

- The Foundry agent's system instructions were a hardcoded `const` string inside
  `FoundryAgentGateway.cs` — versioned only implicitly via that file's git history.

New state:

- The prompt is a first-class asset: `dotnet/src/HomeScoutCopilot.API.Service/Prompts/
  homescout.v1.md`, embedded in the assembly and loaded by `AgentPrompt` (with a `Version`
  constant). `FoundryAgentGateway` now passes `AgentPrompt.Instructions`. A fast
  `AgentPromptTests` locks the load path + guardrails.

Reason:

- Aligns with Microsoft's GenAIOps prompt-versioning guidance — treat prompts as
  first-class, reviewable, git-taggable code assets (version by filename `vN`, tag the
  deploy that ships each version):
  [repo structure](https://learn.microsoft.com/en-us/training/modules/prompt-versioning-genaiops/4-github-repository-structure)
  · [prompt workflow](https://learn.microsoft.com/en-us/training/modules/prompt-versioning-genaiops/5-prompt-workflow-development).

Notes / not-yet:

- The module's mechanics are Python-SDK-oriented (`.txt` read by a deploy script) and its
  "Foundry auto-increments agent versions" note assumes a **persisted** Foundry agent
  resource. HomeScout's gateway builds an **in-process** agent via `AIProjectClient.AsAIAgent(...)`
  (Responses path) — nothing is persisted server-side — so our prompt versioning is
  file + git-tag, not server-side agent versions. Prompt-quality **evaluation** and
  dev/prod prompt separation remain follow-ups.

### 2026-07-03: API Vertical Slices — Project Roles, Options Helper, Shared Rename

RagLab shape:

- `.API` is the feature **library** (Features/Domain/Infrastructure/Settings); `.API.Service`
  is the **host**. The validated-options helper lives in the shared application project.

HomeScout direction (implemented):

- Adopted RagLab's **internal** organisation — `Features/<X>/` Carter+MediatR vertical slices
  and the `IValidatedOptions` convention — but **kept HomeScout's project roles**: `.API` is
  the host, `.API.Service` is the application layer.
- The validated-options helper lives in **`.API.Service/Settings/`** (not the shared project),
  so `HomeScoutCopilot.Shared` stays **pure wire contracts** (no FluentValidation/DI deps).
- Renamed `HomeScoutCopilot.Shared.Application` → **`HomeScoutCopilot.Shared`** (project +
  namespace `HomeScoutCopilot.Shared.Contracts`).
- Did **not** add a request-level FluentValidation pipeline yet; existing in-handler checks
  preserve behaviour (a documented follow-up).

Reason:

- Keeps the layering we already have, keeps contracts dependency-light, and delivers the
  navigability win without inverting project roles or risking behaviour drift.

Impact:

- Behaviour unchanged — all contract/BDD/endpoint tests pass unedited. MediatR pinned to
  **12.5.0** (last free/Apache-2.0; v13+ is commercial).

### 2026-07-03: Basic Foundry Agent Setup (Not RagLab's Standard) For The First Slice

RagLab / reference expectation:

- RagLab uses the **Standard** Foundry agent setup: bring-your-own Azure Cosmos DB
  (thread storage) + Azure Storage + Azure AI Search, wired through account and project
  capability hosts.

HomeScout direction (for now):

- The first conversational cost-answer slice uses the **Basic** agent setup —
  Microsoft-managed thread/file/vector storage; no capability host, no Cosmos.

Reason:

- Verified against Microsoft docs (capability hosts / standard agent setup) that there
  is **no Cosmos-only** option — the project capability host requires Cosmos **and**
  AI Search **and** Storage together, or none (all Microsoft-managed). The cost-answer
  slice needs none of tenant-owned threads, RAG, or file uploads yet, so Basic ships it
  with minimal infra and cost.

Impact:

- Threads are stored in Microsoft's managed store, not our tenant, until we upgrade.
- **Upgrade to Standard** (Cosmos ≥3000 RU/s + Storage + AI Search + Key Vault + two
  capability hosts + RBAC) as a dedicated step before data residency / server-side
  thread persistence / RAG matter, grounded in `foundry-samples/43-standard-agent-setup`
  + RagLab. Document Intelligence is separate (RAG) and deferred independently.

### 2026-07-02: Aspire Starter Instead Of Exact Companion Repo Shape

Course/plan expectation:

- Track the course and companion repo closely.

Actual implementation:

- Created HomeScout with `dotnet new aspire-starter`, which initially included Blazor web, API service, AppHost, ServiceDefaults, and tests.
- This was later superseded by the React-from-Part-1 pivot.

Reason:

- The starter gives a working product-oriented scaffold while staying close to the course's Aspire direction.

Impact:

- The old Blazor project and starter UI were removed during the React pivot.
- Remaining starter influence is limited to Aspire project structure, service defaults, tests, and health endpoints.

### 2026-07-02: Wiki Canonical Plan Root

Previous state:

- Plans lived under `docs/`.
- A temporary compatibility layer linked `docs/` paths into `wiki/plan/`.

New state:

- Canonical plans live under `wiki/__plans/`.
- The `docs/` compatibility layer has been removed.
- Plan divergence checks should read `wiki/__plans/` directly.

Reason:

- This is a solo project, so duplicate documentation paths add friction without helping coordination.
- The project now uses `wiki/` as the single development memory.


### 2026-07-02: API-First Foundry Agent Direction

Course/companion shape:

- Blazor Server page directly creates and runs agents through `AzureOpenAIAgentFactory`.
- There is no separate API-first backend in the companion repo.

HomeScout direction:

- HomeScout will be API-first.
- React will call `HomeScoutCopilot.ApiService`.
- Agent orchestration will sit behind an API-owned gateway abstraction.
- The target enterprise agent platform is Microsoft Foundry Agent Service.
- Direct model calls should use the OpenAI SDK against Foundry's `/openai/v1` endpoint.
- Project, agent, index, evaluation, and tracing platform work should use the Foundry SDK.
- The course repo's local Azure OpenAI agent factory is reference material, not the implementation target.

Reason:

- This better supports a future React frontend, public-data integrations, testing, security boundaries, and enterprise deployment patterns.
- Microsoft Foundry Agent Service provides managed identity, RBAC, observability, private networking options, guardrails, publishing/versioning, and hosted agent endpoints.

Impact:

- Course code should be adapted rather than copied when it places agent logic in Blazor components.
- Part 3 and later should introduce API/service boundaries before agent-specific details where practical.
- Before adding Foundry packages, check current Microsoft docs and pin the intended OpenAI SDK, Foundry SDK, and Azure Identity package versions explicitly.

### 2026-07-02: React From Part 1

Previous state:

- HomeScout was initially scaffolded with the Aspire Blazor starter.
- The plan kept React as a likely later frontend option.

New state:

- HomeScout uses React/Vite from the first implementation part.
- The Blazor project has been removed.
- The course's Blazor code remains reference material only.
- The frontend calls `HomeScoutCopilot.ApiService` and does not own agent orchestration.

Reason:

- The product should start with the eventual frontend stack instead of building a Blazor UI and rewriting later.
- React better matches the desired client direction while the API-first boundary preserves course learning and future Foundry integration.

Impact:

- Part 2 maps the course's Blazor baseline into a React comparison workspace.
- Course UI patterns should be translated into React components and API calls.
- HomeScout implementation commits should avoid adding new Blazor code unless explicitly requested.

### 2026-07-02: Case-File RAG Instead Of Generic Document Chat

Course/typical RAG expectation:

- A product might index a static document corpus and provide generic "chat with docs" behavior.

HomeScout direction:

- HomeScout uses a user-owned case file for private, per-comparison evidence.
- HomeScout also uses a curated knowledge base of authored product notes, terminology, assumptions, source guidance, and safety rules.
- Live public data remains behind tools/API integrations rather than becoming stale indexed documents.

Reason:

- Homebuying decisions depend on a mix of uploaded property evidence, stable explanatory guidance, deterministic calculations, live public data, and explicit missing-information handling.

Impact:

- RAG work enters the plan gradually: curated knowledge starts in Phase 2-3, case-file scoping starts in Phase 5, retrieval arrives in Phase 6, and Azure search/storage planning arrives in Phase 7.

### 2026-07-05: `POST /api/comparison` Supersedes The `/api/comparisons/draft` Placeholder

Plan expectation:

- Phase 2 of [Phased Learning And Build Plan](./phased-learning-build-plan.md) named the comparison
  endpoint `POST /api/comparisons/draft` (a vague placeholder).

HomeScout direction:

- The real endpoint is `POST /api/comparison` (singular), consistent with the existing
  `Features/Comparison` slice and the `/api/comparison/sample` route it replaces. Designed in
  [Listing Model + Comparison Spine](../03-backend/listing-decision-pack-plan.md).

Reason:

- Consistency with the already-implemented `/api/comparison/...` prefix and feature folder; avoids a
  gratuitous plural/singular split. The phased plan text was updated in the same change.

Impact:

- `/api/comparisons/draft` never shipped; no code referenced it. `/api/comparison/sample` (placeholder)
  is removed by this slice.
