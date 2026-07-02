# API-First Foundry Agents

This page records the HomeScout architecture decision to be API-first and to target Microsoft Foundry Agent Service for the agent layer.

## Decision

HomeScout Copilot will use an API-first architecture:

```text
React Web UI
  -> HomeScout API
  -> Foundry agent gateway / tool services
  -> Microsoft Foundry Agent Service and public-data integrations
```

The React frontend must not own agent orchestration. It should call backend endpoints and render workspace state.

## Why

The course companion repo is Blazor-first: the Blazor Server page directly creates and runs agents through `AzureOpenAIAgentFactory`. That is useful for learning, but HomeScout is a product-oriented app and should preserve a backend boundary from the beginning.

Microsoft's current Foundry Agent Service direction is more enterprise-aligned because it provides:

- managed agent runtime options
- Foundry project endpoints
- Responses API as a common entry point
- hosted agents for code-based agents
- prompt agents for managed configuration-based agents
- Microsoft Entra identity and RBAC
- observability and tracing
- private networking options
- content safety and guardrails
- publishing/versioning capabilities

## Important Nuance

Do not use the old/classic Foundry agents path for new work. Microsoft documentation says classic agents are deprecated and scheduled for retirement. HomeScout should target the newer Microsoft Foundry Agent Service APIs and concepts.

## Preferred HomeScout Shape

### Frontend

`frontend`

Responsibilities:

- comparison workspace UI
- saved search UI
- upload UI
- preference UI
- streaming report display

The frontend calls API endpoints. It does not create Foundry agents directly.

### API

`HomeScoutCopilot.ApiService`

Responsibilities:

- API endpoints for comparison sessions
- public-data tool endpoints
- upload handling endpoints
- user-scoped saved search APIs
- agent gateway abstraction
- safety/policy checks before and after agent calls

### Agent Gateway

Create an interface before binding to Foundry directly, for example:

```csharp
public interface IHomeScoutAgentGateway
{
    Task<ComparisonResponse> CreateComparisonAsync(ComparisonRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<ComparisonUpdate> StreamComparisonAsync(ComparisonRequest request, CancellationToken cancellationToken);
}
```

Possible implementations:

- `StubHomeScoutAgentGateway` for early UI/API work.
- `LocalFoundryResponsesAgentGateway` using Foundry Responses API from our API process.
- `HostedFoundryAgentGateway` calling a deployed Foundry hosted agent endpoint.

## Phased Plan

### Phase 1: API-First Local Boundary

- Move comparison behavior behind `HomeScoutCopilot.ApiService` endpoints.
- Keep React as a client of the API.
- Add request/response DTOs.
- Add tests around deterministic API behavior.

### Phase 2: Agent Gateway Abstraction

- Add `IHomeScoutAgentGateway`.
- Use a stub implementation first.
- Add a deterministic cost-estimation tool as the first tool.

### Phase 3: Foundry Responses API

- Add a Foundry-backed implementation that calls the Foundry project endpoint from `ApiService`.
- Keep local code running outside Foundry at first.
- Use this as the lowest-friction way to learn Foundry models/tools without deploying hosted containers immediately.

### Phase 4: Hosted Foundry Agent

- Package HomeScout agent code as a hosted agent when the core workflow stabilizes.
- Use a managed Foundry endpoint, versioning, identity, and observability.
- Keep `ApiService` as the frontend-facing product API, even if the agent runs in Foundry.

## Course Mapping Impact

We still follow the course step by step, but translate implementation boundaries:

| Course Pattern | HomeScout Translation |
| --- | --- |
| Course Blazor page creates agent | API service calls agent gateway |
| Course Blazor page owns tools | API/tool layer owns tools |
| Course Blazor conversation storage | API-owned comparison session storage |
| Course Blazor streaming response | API streams report updates to frontend |
| Local Azure OpenAI factory | Foundry Responses API or hosted Foundry agent |

## Open Questions

- Should the first Foundry integration use a prompt agent or direct Responses API from `ApiService`?
- When do we deploy a hosted agent versus keeping agent code in `ApiService`?
- Which HomeScout tools should be custom functions versus MCP tools?
- Which data should live in our database versus Foundry/session state?

## Working Rule

When the course puts logic directly into Blazor, HomeScout should usually place that logic behind an API/service boundary unless the logic is purely presentation state.

## Fact-Check Sources

Checked on 2026-07-02:

- Microsoft Learn: What is Microsoft Foundry Agent Service? https://learn.microsoft.com/en-us/azure/foundry/agents/overview
- Microsoft Learn: Deploy a hosted agent https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/deploy-hosted-agent
- Microsoft Learn: Classic agents deprecation note https://learn.microsoft.com/en-us/azure/foundry-classic/agents/quickstart
