# Copilot Agent Gateway — Design (Foundry Agent Service)

This is the design for HomeScout's **copilot**: the agent layer that turns a
natural-language request into tool calls + a grounded, cited, safe answer. It is the
point of the product — the deterministic tools (mortgage estimator, base rate) are
**capabilities the copilot calls**, not the product itself.

Anchors: [[API-First Foundry Agents]], [[RAG Architecture]], and the phased plan's
"Prompt Governance And First Agent Workflow" phase.

## Product framing

A buyer talks to HomeScout in natural language. The agent **reasons about what's
needed, calls tools, gathers evidence, and synthesises an explainable answer** with a
trail that separates *facts / estimates / assumptions / missing data*, inside the
"not mortgage advice / no safe-unsafe label" boundary. The frontend is a
**conversation surface**, not a form.

**First slice — conversational cost answer:** *"What would the monthly cost be on a
£300k flat with 10% down?"* → the agent calls the **mortgage-estimate** tool (and
**base-rate** for context) → a grounded answer with the figure, assumptions, and the
not-mortgage-advice caveat. This uses only tools we already have.

## Decisions (locked)

- **Real Foundry via the new Microsoft Agent Framework** (1.0 GA) — not a stub, and
  **not** the classic `PersistentAgentsClient` (`Azure.AI.Agents.Persistent`), which is
  being phased out. Packages: `Microsoft.Agents.AI` + `Microsoft.Agents.AI.Foundry` +
  `Azure.AI.Projects` + `Azure.Identity`. Create the agent with
  `AIProjectClient.AsAIAgent(...)` / `CreateAIAgent(model, instructions, name)`; tools
  are plain C# methods wrapped with `AIFunctionFactory.Create(...)`, and the framework
  runs the tool-call loop.
- **Reproducible provisioning** via **azd + bicep** (`azd up`), per-environment. This
  is the real need that justifies `dotnet/infra/` + `azure.yaml` (the earlier
  "no premature scaffolding" rule deferred infra *until* a real need — this is it).
- **Companion repo = inspiration only** (it uses the Agent Framework / Azure OpenAI
  directly; we target Foundry Agent Service). Do not copy its shape.
- **Verify, don't assume:** offline tests with a fake gateway run in the PR gate; the
  real Foundry path is proven by a live `[Category("External")]` test that runs where
  Azure credentials exist (not in the dev sandbox). We do not mark the live path
  verified until it has run against real Foundry.

## Request flow

```text
React (conversation)  ->  POST /api/copilot/ask
  -> HomeScoutCopilot.API (thin endpoint)
  -> IHomeScoutAgentGateway (.API.Service)
       -> FoundryAgentGateway: AIProjectClient(projectEndpoint, DefaultAzureCredential)
          .AsAIAgent(modelDeployment, instructions) with tools:
            AIFunctionFactory.Create(estimate_mortgage -> IMortgageCostEstimator)
            AIFunctionFactory.Create(get_base_rate     -> IBaseRateProvider)
          agent.RunAsync(message) — the Agent Framework runs the tool-call loop
  -> CopilotAnswer { text, toolCalls[], assumptions[], caveats[] }
```

The agent decides *which* tools to call; **we execute them** (our tested, deterministic
services) and submit the results. The answer carries the tool calls made, so the
evidence trail is explicit — we can see the agent reasoned with tools, not hallucinated.

## Tools exposed to the agent

Registered as `AIFunction`s via `AIFunctionFactory.Create(...)` over the already-built,
tested services (the framework marshals args and invokes them):

- `estimate_mortgage(propertyPrice, deposit, annualInterestRatePercent, termYears, repaymentType)`
  → `MortgageEstimateResult` (via `IMortgageCostEstimator`).
- `get_base_rate()` → `BaseRate` (context only; via `IBaseRateProvider`).

New tools (crime, amenities, schools, price, RAG retrieval) register the same way as
they are built.

## Agent instructions (prompt governance)

The system instructions encode the safety boundary and behaviour: use tools for any
number (never invent a rate — the rate is the buyer's input, or ask for it); always
return assumptions + the not-mortgage-advice caveat; no product recommendation; no
simplistic safe/unsafe area label. The prompt is a **versioned file** (first entry of
the prompt inventory) so changes are reviewable — ties to the phased plan's prompt
governance phase.

## Reproducible provisioning (azd + bicep)

Resources for the cost-answer slice (informed by RagLab's bicep):

- **Foundry account** — `Microsoft.CognitiveServices/accounts`, kind `AIServices`, SKU
  `S0`.
- **Chat model deployment** — `Microsoft.CognitiveServices/accounts/deployments`. The
  deployment name is a stable role label (e.g. `chat`) decoupled from the underlying
  model, so swapping models is a deploy-param change, not a code change. Mind SKU/quota:
  regional `Standard` has default quota; `GlobalStandard` (e.g. gpt-5-*) often starts at
  0 and needs a quota grant.
- **Foundry project** — `Microsoft.CognitiveServices/accounts/projects`, system-assigned
  identity. Deploy the **project as a separate module *after* the account + model
  deployment settle** (avoids a `RequestConflict` provisioning error — RagLab's hard-won
  lesson).
- **RBAC** — the API's managed identity gets the Foundry user/developer role on the
  account; a deployer assignment covers local dev.

Deferred (added with their phases, as RagLab has them): **Cosmos DB** (agent thread
storage) when we add server-side conversation persistence; **Azure AI Search +
Document Intelligence + Storage** when RAG / document upload land. The cost-answer slice
needs none of these.

- `azure.yaml` + `azd up` provision + deploy reproducibly; each environment has its own
  `.env`. Reference `Azure-Samples/azd-ai-starter-basic` + the azd AI agent extension.
- **Local dev:** `az login` + `DefaultAzureCredential`; config via user-secrets/env
  (project endpoint + model deployment name). No secrets committed.

## Config & identity

`FoundryOptions { ProjectEndpoint, ModelDeploymentName }` bound from config.
`DefaultAzureCredential` — managed identity in Azure, `az login` locally. SDK:
**Microsoft Agent Framework** (`Microsoft.Agents.AI`, `Microsoft.Agents.AI.Foundry`,
`Azure.AI.Projects`, `Azure.Identity`) — pin exact versions at implementation time
(verify on Microsoft Learn). The classic `PersistentAgentsClient`
(`Azure.AI.Agents.Persistent`) is being phased out; do not use it.

## API, client, DTOs

- `POST /api/copilot/ask` → body `{ message, context? }`.
- `CopilotAnswer { string Text, IReadOnlyList<ToolCall> ToolCalls, IReadOnlyList<string> Assumptions, IReadOnlyList<string> Caveats }`.
- `HomeScoutApiClient.AskAsync`.
- Streaming is a later slice (agent run streaming → SSE → React); first slice returns
  the completed answer.

## Verification strategy (verify, don't assume)

- **Offline / fast gate:** a `FakeAgentGateway` returns a deterministic answer; unit
  tests prove the **tool-execution wiring** (an `estimate_mortgage` tool call routes
  to `IMortgageCostEstimator` and returns the correct figure). No Azure, no network.
- **Live:** `FoundryAgentGatewayLiveTests` `[Category("External")]` runs a real
  cost-answer against provisioned Foundry and asserts the answer contains the
  tool-derived monthly figure + the caveat. Runs in a scheduled/creds-gated workflow
  (extend `external-checks.yml` or add `azure-checks.yml`) — **not** the PR gate, and
  **not** runnable from the dev sandbox. This is the proof the live path works.
- **Observability:** the agent run + tool calls are traced (OTel via ServiceDefaults);
  `CopilotAnswer.ToolCalls` makes the evidence trail explicit.

## Implementation slices

1. **Infra** — `azure.yaml` + bicep for Foundry project + model deployment + RBAC;
   `azd up`-able. Verified by running `azd up` (user/CI with Azure access).
2. **Gateway + tools + offline tests** ✅ done — `IHomeScoutAgentGateway`,
   `CopilotRequest`/`CopilotAnswer` DTOs, `HomeScoutAgentTools` (real `AIFunction`s via
   `AIFunctionFactory.Create` over the estimator + base rate), and a `FakeHomeScoutAgentGateway`
   test double. Tool tests invoke the `AIFunction`s directly (offline, no Azure) and
   assert they route to the services. Fast gate green.
3. **FoundryAgentGateway** — `AIProjectClient.AsAIAgent()` + `AIFunctionFactory` tools
   (the Agent Framework runs the tool loop) + live `External` test + creds-gated
   workflow. Verified against real Foundry where creds exist.
4. **Endpoint + client** — `POST /api/copilot/ask` + typed client; then the React
   conversation surface (separate slice).

## Out of scope now

Streaming, RAG retrieval (case-file / curated KB), hosted-agent deployment, memory,
and voice — each a later slice once the first tool-calling loop is proven.

## Sources

- Microsoft Agent Framework overview (the agent SDK): https://learn.microsoft.com/en-us/agent-framework/overview/
- Agent Framework — Microsoft Foundry provider (.NET, `AsAIAgent`/`AIFunctionFactory`): https://learn.microsoft.com/en-us/agent-framework/agents/providers/microsoft-foundry
- Agentic app with Agent Framework or Foundry Agent Service (.NET tutorial): https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-agent-web-app-semantic-kernel-foundry-dotnet
- Foundry Agent Service function calling (concepts): https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/function-calling
- What is Foundry Agent Service: https://learn.microsoft.com/en-us/azure/foundry/agents/overview
- Foundry SDK overview (C#): https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview
- azd Azure AI Foundry extension: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension
- azd AI starter template: https://github.com/Azure-Samples/azd-ai-starter-basic
- Deploy models with CLI + Bicep: https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/create-model-deployments
