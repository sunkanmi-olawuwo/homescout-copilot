# Plan Divergence

Use this page whenever implementation differs from:

- the course companion repo
- a playlist step
- an existing product plan
- an existing wiki page

## Entries

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
