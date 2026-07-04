# Component Architecture

## Solution Layout

```text
dotnet/
  HomeScoutCopilot.slnx
  src/
    HomeScoutCopilot.AppHost            (Aspire orchestration)
    HomeScoutCopilot.ServiceDefaults    (shared Aspire defaults)
    HomeScoutCopilot.API                (host; Carter Features/ slices -> MediatR handlers)
    HomeScoutCopilot.API.Service        (application layer; services + Settings/ validated options)
    HomeScoutCopilot.API.Client         (typed HTTP client over the API)
    HomeScoutCopilot.Shared             (DTOs / wire contracts)
    HomeScoutCopilot.Functional         (FluentResults -> ProblemDetails mappers)
  tools/                                (operational .NET console tools; see GenAIOps
    HomeScoutCopilot.AgentOps           Tooling Plan. NOT in the API runtime.)
    HomeScoutCopilot.Evaluator          (AgentOps: manifest step done. Evaluator: safety
                                         evals done; Foundry cloud evals next.)
  tests/
    HomeScoutCopilot.API.Test           (NUnit contract + Aspire integration + Reqnroll BDD)
    HomeScoutCopilot.Shared.Test
    HomeScoutCopilot.Functional.Test
    HomeScoutCopilot.AgentOps.Test
    HomeScoutCopilot.Evaluator.Test
frontend/         (React/Vite, at repo root)
```

The `tools/` folder holds operational console tools. `HomeScoutCopilot.AgentOps` (declarative
manifest done; live `CreateAgentVersion` next) and `HomeScoutCopilot.Evaluator` (`evaluator
safety` runs deterministic guardrail evals over a version-controlled dataset; Foundry cloud
evals next). They reference `.API.Service` to reuse the single-sourced agent definition and
are excluded from the API runtime.

.NET projects live under `dotnet/` (RagLab skeleton parity); the React frontend
stays at the repo root. The `AppHost` references the frontend at `../../../frontend`
and the API resource is still named `apiservice` (so the Vite proxy env is stable).

### Request flow

```text
React (frontend)  ->  HomeScoutCopilot.API (endpoints)
                        -> HomeScoutCopilot.API.Service (IHomeScoutService, returns Result<T>)
                        -> HomeScoutCopilot.Functional (Result -> IResult / ProblemDetails)
Shared DTOs (HomeScoutCopilot.Shared) are used by API, API.Client, and tests.
```

Endpoints stay thin: they call `IHomeScoutService` and map the `Result<T>` to HTTP
via `.ToHttpResult()`. Expected failures become ProblemDetails, not exceptions.

## AppHost

Project: `HomeScoutCopilot.AppHost`

Responsibilities:

- Aspire orchestration.
- Wiring service references.
- Local development entry point.
- Future environment configuration for AI/data API keys.

Current state:

- Adds `HomeScoutCopilot.ApiService` as `apiservice`.
- Adds `frontend` as a Vite `webfrontend` resource.
- Makes the React frontend wait for the API service.

## ServiceDefaults

Project: `HomeScoutCopilot.ServiceDefaults`

Responsibilities:

- Shared health checks.
- OpenTelemetry defaults.
- Service discovery defaults.
- Common resilience behavior from the Aspire template.

## Frontend

Project: `frontend`

Responsibilities:

- React/Vite frontend.
- User workspace for property and area comparison.
- Future chat UI, upload controls, saved comparisons, and preference views.

Current state (iteration 2, built to the Claude Design):

- Scoped light/dark design tokens + IBM Plex fonts; the three-region workspace from the
  design — navy app bar, left rail (New comparison, saved-search filter, saved comparisons,
  Case file / Preferences / Settings), main surface, right rail. Mobile (<760) collapses the
  left rail into a hamburger drawer, content-first.
- The **copilot conversation is the main surface** (status pill, heading, intro, START WITH
  suggestion cards, composer, inline caveat). The composer and START WITH cards post to
  `/api/copilot/ask`; successful `CopilotAnswer` responses render the answer text, tool chips,
  assumptions and caveats. Once a conversation starts, the large intro collapses to a compact
  header so the answer remains the focus. Copilot prose is rendered from a small sanitized
  Markdown subset (no raw HTML injection; unsafe links become text). The UI still **degrades
  gracefully on 503** for environments without Foundry configuration.
- The right-rail **Evidence** tab hydrates from `CopilotAnswer.Evidence`, rendering each
  figure's lowercase `kind`, Live/Cache/Fallback provenance and source.
- The **mortgage estimator is a right-rail panel** (Evidence | Estimator tabs) wired to
  `/api/mortgage/estimate` + `/api/mortgage/base-rate`: monthly payment, loan, LTV, total
  interest, total repayable, +3% stress payment, and Live/Cache base-rate provenance. Every
  figure carries a fact/estimate/assumption/missing tag; the not-mortgage-advice caveat is
  always shown.
- Verified against the design (light/dark, desktop/mobile) and the live API end-to-end.
- The old Blazor project has been removed.

Frontend work must follow [[Frontend Design Guidelines]] and, for building from the design,
the Codex handoff in `wiki/__plans/02-frontend/codex-frontend-instructions.md`.

## API and application layer

Split into layered projects (RagLab parity):

- `HomeScoutCopilot.API` — host. Endpoints are **vertical slices** under `Features/<X>/`
  as **Carter** `ICarterModule`s (auto-discovered) that delegate to **MediatR**
  commands/queries/handlers; handlers call the `.API.Service` services and map their
  `Result<T>` to HTTP via `.ToHttpResult()`. `Program.cs` is thin (registers
  Carter/MediatR/validated-options + DI; `app.MapCarter()`). Owns OpenAPI, ProblemDetails,
  static files, Aspire defaults.
- `HomeScoutCopilot.API.Service` — application layer. Hosts the copilot boundary
  `IHomeScoutAgentGateway` (with `FoundryAgentGateway` — Microsoft Agent Framework over
  `AIProjectClient.AsAIAgent`, keyless via `DefaultAzureCredential`; its system prompt loads
  from a versioned embedded asset `Prompts/homescout.v1.md` via `AgentPrompt`, not a
  hardcoded string) and `HomeScoutAgentTools` (the estimator + base rate exposed
  as `Microsoft.Extensions.AI` `AIFunction`s for the Foundry agent to call),
  `IMortgageCostEstimator` (pure, deterministic amortisation), and `IHomeScoutService`,
  which returns
  FluentResults; future agent-gateway and tool orchestration land behind this
  interface. Also hosts `IBaseRateProvider` (`BankOfEnglandBaseRateProvider`: live
  BoE base-rate fetch with ~1-day cache and a resilient fallback that never throws).
- `HomeScoutCopilot.API.Client` — typed HTTP client (`HomeScoutApiClient`) over the
  API, consumed by server-to-server callers and the API test project's BDD driver.
- `HomeScoutCopilot.Shared` — DTOs / wire contracts shared by API, client, and tests.
- **Options** live in `.API.Service/Settings/` as `IValidatedOptions<T>` (self-declared
  section + FluentValidation validator), bound and **validated on startup** via
  `AddValidatedOptions<T>()` — bad config fails fast.
- `HomeScoutCopilot.Functional` — FluentResults → `IResult`/ProblemDetails mappers.

Responsibilities of the API/service layer:

- API routes.
- HomeScout data tools.
- Future agent-facing tool wrappers.
- Future public-data integration logic.
- Future user-owned case-file retrieval and curated knowledge-base retrieval.

Planned tool areas:

- crime lookup
- amenities lookup
- school lookup
- price-paid context
- ownership cost estimate
- commute estimate
- case-file document retrieval
- curated HomeScout knowledge-base retrieval

## Tests

Project: `HomeScoutCopilot.Tests`

Responsibilities:

- Aspire integration tests.
- Future service-level tests for data integrations.
- Future safety/guardrail tests for high-risk responses.

See [[Testing Strategy]].

## Architectural Direction

HomeScout is API-first. The course companion repo begins with a Blazor Server chatbot where the page directly creates and runs agents. HomeScout should learn from that implementation while preserving product-grade boundaries:

- Chat and workspace UI belong in `frontend`.
- Product behavior, data providers, tool wrappers, and agent gateway calls belong in `ApiService` or a future shared/application project.
- React components must not own Foundry agent orchestration.
- Aspire wiring belongs in `AppHost`.
- Cross-service defaults belong in `ServiceDefaults`.

See [[API-First Foundry Agents]].

## RAG Layers

HomeScout uses two retrieval layers:

- User-owned case file: private per-user/per-comparison corpus containing listings, EPCs, surveys, floorplans, viewing notes, estate-agent messages, screenshots, photos, and preferences.
- Curated HomeScout knowledge base: stable product knowledge containing homebuying explainers, EPC and survey terminology, cost assumptions, data-source interpretation notes, safety rules, and source reliability guidance.

See [[RAG Architecture]].
