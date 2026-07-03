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

- **Real Foundry Agent Service** (not a stub, not the lighter OpenAI-SDK path):
  `PersistentAgentsClient` + `DefaultAzureCredential`, function tool-calling.
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
       -> FoundryAgentGateway: PersistentAgentsClient + DefaultAzureCredential
          create agent (model + instructions) -> thread -> run -> poll
          on RequiresAction: execute the requested function tool locally
            estimate_mortgage -> IMortgageCostEstimator
            get_base_rate     -> IBaseRateProvider
          submit tool outputs -> continue until completed
  -> CopilotAnswer { text, toolCalls[], assumptions[], caveats[] }
```

The agent decides *which* tools to call; **we execute them** (our tested, deterministic
services) and submit the results. The answer carries the tool calls made, so the
evidence trail is explicit — we can see the agent reasoned with tools, not hallucinated.

## Tools exposed to the agent

Function tool definitions wrapping the already-built, tested services:

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

- `azure.yaml` + `infra/` bicep: a **Foundry project/account**, a **model deployment**
  (name/version/capacity — pin at implementation time), and **RBAC** for the API's
  managed identity (e.g. Azure AI Developer / Cognitive Services User). Outputs
  (project endpoint, model deployment name) flow into app config. Reference the
  `Azure-Samples/azd-ai-starter-basic` template and the azd AI agent extension.
- `azd up` provisions + deploys reproducibly; each environment has its own `.env`.
- **Local dev:** `az login` + `DefaultAzureCredential`; config via user-secrets/env
  (project endpoint + model deployment name). No secrets committed.

## Config & identity

`FoundryOptions { ProjectEndpoint, ModelDeploymentName }` bound from config.
`DefaultAzureCredential` — managed identity in Azure, `az login` locally. Package:
verify the exact Foundry agent SDK at implementation time (candidates:
`Azure.AI.Agents.Persistent` for `PersistentAgentsClient`, or `Azure.AI.Projects[.Agents]`)
plus `Azure.Identity`.

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
2. **Gateway + tools + offline tests** — `IHomeScoutAgentGateway`, tool definitions,
   `FakeAgentGateway`, wiring unit tests. Fast gate green, no Azure.
3. **FoundryAgentGateway** — `PersistentAgentsClient` run loop + live `External` test
   + creds-gated workflow. Verified against real Foundry where creds exist.
4. **Endpoint + client** — `POST /api/copilot/ask` + typed client; then the React
   conversation surface (separate slice).

## Out of scope now

Streaming, RAG retrieval (case-file / curated KB), hosted-agent deployment, memory,
and voice — each a later slice once the first tool-calling loop is proven.

## Sources

- Foundry Agent Service function calling (C#): https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/function-calling
- What is Foundry Agent Service: https://learn.microsoft.com/en-us/azure/foundry/agents/overview
- Foundry SDK overview (C#): https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview
- azd Azure AI Foundry extension: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension
- azd AI starter template: https://github.com/Azure-Samples/azd-ai-starter-basic
- Deploy models with CLI + Bicep: https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/create-model-deployments
