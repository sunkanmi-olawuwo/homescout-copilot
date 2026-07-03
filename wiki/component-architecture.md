# Component Architecture

## Solution Layout

```text
dotnet/
  HomeScoutCopilot.slnx
  src/
    HomeScoutCopilot.AppHost
    HomeScoutCopilot.ServiceDefaults
    HomeScoutCopilot.ApiService
  tests/
    HomeScoutCopilot.Tests
frontend/         (React/Vite, at repo root)
```

.NET projects live under `dotnet/` (RagLab skeleton parity); the React frontend
stays at the repo root. The `AppHost` references the frontend at `../../../frontend`.

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

Current state:

- React/Vite app customized with a HomeScout comparison workspace shell.
- The old Blazor project has been removed.

Frontend work must follow [[Frontend Design Guidelines]].

## ApiService

Project: `HomeScoutCopilot.ApiService`

Responsibilities:

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
