# Component Architecture

## Solution Layout

```text
HomeScoutCopilot.sln
  HomeScoutCopilot.AppHost
  HomeScoutCopilot.ServiceDefaults
  HomeScoutCopilot.Web
  HomeScoutCopilot.ApiService
  HomeScoutCopilot.Tests
```

## AppHost

Project: `HomeScoutCopilot.AppHost`

Responsibilities:

- Aspire orchestration.
- Wiring service references.
- Local development entry point.
- Future environment configuration for AI/data API keys.

Current state:

- Adds `HomeScoutCopilot.ApiService` as `apiservice`.
- Adds `HomeScoutCopilot.Web` as `webfrontend`.
- Makes the web frontend wait for the API service.

## ServiceDefaults

Project: `HomeScoutCopilot.ServiceDefaults`

Responsibilities:

- Shared health checks.
- OpenTelemetry defaults.
- Service discovery defaults.
- Common resilience behavior from the Aspire template.

## Web

Project: `HomeScoutCopilot.Web`

Responsibilities:

- Blazor frontend.
- User workspace for property and area comparison.
- Future chat UI, upload controls, saved comparisons, and preference views.

Current state:

- Starter Blazor app customized with a HomeScout landing/workspace shell.
- Starter `Counter` and `Weather` pages remain as scaffolding and should be replaced as course-aligned features are implemented.

Frontend work must follow [[Frontend Design Guidelines]].

## ApiService

Project: `HomeScoutCopilot.ApiService`

Responsibilities:

- API routes.
- HomeScout data tools.
- Future agent-facing tool wrappers.
- Future public-data integration logic.

Planned tool areas:

- crime lookup
- amenities lookup
- school lookup
- price-paid context
- ownership cost estimate
- commute estimate

## Tests

Project: `HomeScoutCopilot.Tests`

Responsibilities:

- Aspire integration tests.
- Future service-level tests for data integrations.
- Future safety/guardrail tests for high-risk responses.

See [[Testing Strategy]].

## Architectural Direction

The course companion repo begins with a Blazor Server chatbot. HomeScout should keep that learning path while adding product-specific boundaries:

- Chat and workspace UI belong in `Web`.
- Data providers and tool wrappers belong in `ApiService` or a future shared/application project.
- Aspire wiring belongs in `AppHost`.
- Cross-service defaults belong in `ServiceDefaults`.

