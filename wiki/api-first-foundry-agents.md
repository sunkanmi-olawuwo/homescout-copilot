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

## SDK/API Rule

HomeScout should use the current Microsoft Foundry Agent Service SDK/API surface for implementation, not the course repo's older local Azure OpenAI agent factory pattern.

The implementation path is anchored to Microsoft's current Foundry SDK overview for C#:

- Use the new Microsoft Foundry Agent Service, not classic agents.
- Use the OpenAI SDK path for direct model calls through Foundry's `/openai/v1` endpoint, including responses, chat completions, embeddings, and latency-sensitive model calls.
- Use the Foundry SDK path for Foundry project/resource operations, agents, indexes, evaluation, and tracing-oriented platform work.
- Use Microsoft Entra authentication through `DefaultAzureCredential` where supported.
- Do not use `Azure.AI.Agents.Persistent` as the default plan unless current implementation-time docs show it is required for the selected Foundry path.

For C#, the Foundry SDK overview currently points to packages such as:

- `Azure.AI.Projects`
- `Azure.AI.Projects.Agents`
- `Azure.AI.Extensions.OpenAI`
- `Azure.Identity`

The first implementation can still use a stub gateway, but the real Foundry implementation should be built against the new Foundry project endpoints and the current recommended SDK/API packages from those docs.

## Why

The course companion repo is Blazor-first: the Blazor Server page directly creates and runs agents through `AzureOpenAIAgentFactory`. That is useful for learning concepts, patterns, and standard C# implementation habits, but HomeScout is a product-oriented app and should preserve a backend boundary from the beginning.

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

Do not use the old/classic Foundry agents path for new work. Microsoft documentation says classic agents are deprecated and scheduled for retirement. HomeScout should target the newer Microsoft Foundry Agent Service SDKs, APIs, project endpoints, and concepts.

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
- `FoundryOpenAiGateway` using the OpenAI SDK against Foundry's `/openai/v1` endpoint from our API process.
- `FoundryProjectAgentGateway` using the Foundry SDK for project/agent orchestration, if we choose that path.
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

### Phase 3: Foundry SDK Integration

- Add a Foundry-backed implementation that calls the Foundry project endpoint from `ApiService` using the current SDK/API surface.
- Keep local code running outside Foundry at first.
- Prefer the OpenAI SDK path first for direct model calls.
- Use the Foundry SDK when the work needs project, agent, index, evaluation, or tracing features.
- Use this as the lowest-friction way to learn Foundry models, agents, and platform tools without deploying hosted containers immediately.

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
| Local Azure OpenAI factory | Current Foundry Agent Service SDK/API or hosted Foundry agent |

## Open Questions

- Should the first Foundry integration use direct OpenAI SDK calls through Foundry, or a Foundry SDK agent/project flow?
- When do we deploy a hosted agent versus keeping agent code in `ApiService`?
- Which HomeScout tools should be custom functions versus MCP tools?
- Which data should live in our database versus Foundry/session state?
- Confirm the exact SDK/API route and package versions at implementation time before adding dependencies.

## Working Rule

When the course puts logic directly into Blazor, HomeScout should usually place that logic behind an API/service boundary unless the logic is purely presentation state.

## Fact-Check Sources

Checked on 2026-07-02:

- Microsoft Learn: What is Microsoft Foundry Agent Service? https://learn.microsoft.com/en-us/azure/foundry/agents/overview
- Microsoft Learn: Foundry SDK overview, OpenAI SDK section https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview?pivots=programming-language-csharp#openai-sdk
- Microsoft Learn: Foundry SDK overview, Foundry SDK section https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview?pivots=programming-language-csharp#foundry-sdk
- Microsoft Learn: Deploy a hosted agent https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/deploy-hosted-agent
- Microsoft Learn: Classic agents deprecation note https://learn.microsoft.com/en-us/azure/foundry-classic/agents/quickstart
