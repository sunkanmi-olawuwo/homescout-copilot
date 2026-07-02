# Plan Divergence

Use this page whenever implementation differs from:

- the course companion repo
- a playlist step
- an existing product plan
- an existing wiki page

## Entries

### 2026-07-02: Aspire Starter Instead Of Exact Companion Repo Shape

Course/plan expectation:

- Track the course and companion repo closely.

Actual implementation:

- Created HomeScout with `dotnet new aspire-starter`, which includes Blazor web, API service, AppHost, ServiceDefaults, and tests.

Reason:

- The starter gives a working product-oriented scaffold while staying close to the course's Aspire direction.

Impact:

- Some starter pages such as Counter and Weather exist temporarily.
- These should be replaced as each course feature maps into HomeScout.

### 2026-07-02: Wiki Canonical Plan Root

Previous state:

- Plans lived under `docs/`.
- A temporary compatibility layer linked `docs/` paths into `wiki/plan/`.

New state:

- Canonical plans live under `wiki/plan/`.
- The `docs/` compatibility layer has been removed.
- Plan divergence checks should read `wiki/plan/` directly.

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
- The target enterprise agent platform is Microsoft Foundry Agent Service, not the older classic agents path.

Reason:

- This better supports a future React frontend, public-data integrations, testing, security boundaries, and enterprise deployment patterns.
- Microsoft Foundry Agent Service provides managed identity, RBAC, observability, private networking options, guardrails, publishing/versioning, and hosted agent endpoints.

Impact:

- Course code should be adapted rather than copied when it places agent logic in Blazor components.
- Part 3 and later should introduce API/service boundaries before agent-specific details where practical.

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
