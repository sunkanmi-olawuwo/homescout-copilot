# Server-Side Tools Plan (OpenAPI Tools on the Foundry Agent)

**Status:** Design complete, **deferred by decision (2026-07-04)** — not being built now.

**Why deferred:** the strongest driver (agent-target cloud eval) was already met a better way
(**BYO-responses**, which evaluates the real product), instructions are single-sourced, and the
agent already shows in the portal (CreateAgentVersion). Server-side tools would add a public inbound
surface + managed-identity auth + a dev-tunnel to test the full loop locally — cost that isn't
justified yet. In-process tools are fully offline-testable, secure, and arguably *more* API-first
(our API owns orchestration). `AgenticAICore` confirms an in-process ("Local") agent is a
first-class strategy, and **runtime reasoning tuning is achievable in-process** (done — see [[log]]),
so the one remaining runtime gain from server-side tools is covered. **Revisit only** when the
portal playground must compute or a non-API client consumes the agent. The design below stays
ready to execute if that need arrives.

**Owning phase:** Backend follow-up to the GenAIOps agent work
([[GenAIOps Tooling Plan]], [[API-First Foundry Agents]]). Prerequisite for **reference-by-name**.

## Why (what this unblocks)

Today the copilot's tools (`estimate_mortgage`, `get_base_rate`) are **local C# functions** the API
runs in-process. That works for the API-served agent, but it means:

- The **persisted** Foundry agent has **no tools**, so serving it (reference-by-name) makes the
  model stop calling `estimate_mortgage` — proven by a live spike, 2026-07-04 (see [[log]]).
- The **portal playground** can't compute, and **agent-target cloud eval** would score tool-less
  answers.

Moving the tools **onto the agent definition** as **OpenAPI tools** the Foundry service calls makes
the agent self-contained. This unblocks:

1. **Reference-by-name** (the API can then serve the persisted agent without losing tool-calling).
2. **Agent-target cloud eval** (the mslearn pattern) and the **portal playground**.

It stays consistent with the **API-first invariant**: the Foundry service calls *into the HomeScout
API's* tool endpoints — agent work still sits behind the API boundary; the tools reuse the same
deterministic FluentResults services, and the not-mortgage-advice guardrails still apply.

## Decision: managed identity (Entra), not API keys

Foundry OpenAPI tools support three auth types — **anonymous**, **API key** (via a Foundry
`custom keys` connection), and **managed identity (Microsoft Entra ID)**. **Microsoft's recommended
production approach is managed identity**, and it matches `CLAUDE.md`'s "Microsoft Entra identity
with managed identity and least-privilege." We will use **managed identity**.

- **Chosen:** managed identity. The Foundry project's **system-assigned managed identity** obtains an
  Entra token for our API's **audience** (the API's app-registration Application ID URI); our API
  validates the JWT. No secrets to store or rotate.
- **Not** API key: it's a shared secret to store in a Foundry connection and rotate — avoidable.
- **Not** anonymous: dev-only; unacceptable for a public tool endpoint.

Note this is **service-to-service** auth (Entra managed identity) and is **independent of end-user
auth** ([[Plan Divergence]]: end-user sign-in uses **Keycloak**). The two coexist: Keycloak
authenticates *users* to the app; Entra MI authenticates the *Foundry service* to the tool endpoint.

## Architecture

```
buyer → React → HomeScout API → Foundry agent (persisted, with OpenAPI tools)
                                        │  the model decides to call a tool
                                        ▼
                         Foundry service ──(Entra MI token)──▶ HomeScout API
                                                                 POST /api/agent-tools/estimate-mortgage
                                                                 POST /api/agent-tools/base-rate
                                                                        │ validate JWT (issuer/audience/caller)
                                                                        ▼
                                                             same MortgageCostEstimator / BaseRateProvider
```

The tool **declarations** live in the persisted agent definition (set at provision time). The tool
**execution** is a normal HTTP endpoint on our API — so it uses request-scoped services normally
(this **resolves the scoping question**: server-side tools are declared once, but executed by a
scoped request handler, so there is no captive-dependency problem).

## Endpoint contract (the OpenAPI tool)

A dedicated, minimal, versioned tool surface — separate from the human/UI API — under
`/api/agent-tools/*`. Each operation has an `operationId` (letters/`-`/`_` only, per the spec
requirement) matching the current tool names.

| operationId | Method + path | Request | Response |
| --- | --- | --- | --- |
| `estimate_mortgage` | `POST /api/agent-tools/estimate-mortgage` | `{ propertyPrice, deposit, annualInterestRatePercent, termYears, repaymentType }` | `MortgageEstimate` (or `{ error }`) |
| `get_base_rate` | `GET /api/agent-tools/base-rate` | — | `BaseRate` |

- **Reuse the existing DTOs/services** (`MortgageEstimateRequest`, `IMortgageCostEstimator`,
  `IBaseRateProvider`) — the endpoints are thin adapters over the same tested logic the in-process
  tools use. Single source of truth for the tool behaviour.
- **Guardrails preserved:** the estimator still returns FluentResults failures as a structured
  `{ error }`; the base-rate endpoint keeps the "context only, not a product rate" framing; no tool
  invents a rate (rate is always the buyer's input).
- **Descriptions carry over** from the `[Description]` attributes into the OpenAPI `description`
  fields so the model chooses the right tool.
- Serve the spec at `/api/agent-tools/openapi.json` (or embed it as a versioned asset — TBD at
  implementation) so provisioning references a stable contract.

## Auth model (managed identity) — detail

1. **Register the API in Entra** with an Application ID URI (the **audience**), e.g.
   `api://homescout-agent-tools`. (Or use App Service Easy Auth per the Microsoft "secure OpenAPI
   tool calls" guide if we host on App Service.)
2. **Grant the Foundry project's managed identity** access to that audience (least-privilege — a
   dedicated app role like `AgentTools.Invoke`, not a broad role).
3. **Provision the OpenAPI tool** with `managed_identity` auth + the audience.
4. **Validate on our side**: middleware on `/api/agent-tools/*` validates the bearer JWT —
   issuer = our Entra tenant, audience = our app id URI, and the caller is the **Foundry MI's
   object id** / carries the expected app role. Reject anything else (no anonymous).
5. **Egress:** the Foundry service makes an **outbound** call to our API, so the tool endpoint must
   be **reachable** from the service. Options: (a) public HTTPS endpoint protected by the JWT check
   (simplest); (b) private networking / VNet integration if we want no public exposure (heavier).
   Start with (a) + strict JWT validation + rate limiting; revisit (b) if required. Our tools' own
   egress (Bank of England API) is unchanged.

## Startup check-and-provision (resilience)

The user's resilience idea: on API startup, if the agent isn't registered, provision it.

- **Refactor first:** extract the agent definition + `FoundryAgentDeployer` out of the
  `AgentOps` CLI into a **shared location** (e.g. `HomeScoutCopilot.API.Service` or a small
  `HomeScoutCopilot.Agents` library) so **both** the `agentops deploy` CLI **and** an API startup
  task use the same single-sourced logic (avoids the current AgentOps→API.Service reference cycle).
- **Primary path = CI/ops provisioning** (`agentops deploy` on prompt/tool/model change) — keeps app
  startup fast and doesn't require the app to hold agent-write permissions in every environment.
- **Optional resilience fallback = startup check-and-provision**, behind a config flag
  (`Agent:ProvisionOnStartup`): an `IHostedService`/startup task calls `GetAgentVersions(name)`; if
  missing, runs the deployer. Must be **idempotent** (content-based versioning already gives this)
  and **safe under multiple instances** (a create race is harmless — same content collapses to one
  version; guard with a simple retry/tolerate-conflict).
- **RBAC caveat:** startup provisioning means the app's managed identity needs **agent-write** on
  the project (broader than read-only serving). Prefer CI-provision in production; enable the
  startup fallback in dev / self-healing environments.
- **Tools at provision time (answering the question):** the tool **declarations** (OpenAPI spec +
  auth) go into the definition **at provision time** — static, not scoped. The tool **execution** is
  the scoped API endpoint. So yes, tools are "set up at that point" (provisioning) as declarations,
  while scoped services still run per-request in the endpoint. No captive-dependency issue.

## Security considerations

- **No anonymous** tool endpoint; validate issuer + audience + caller identity/app-role on every
  call.
- **Least-privilege**: a dedicated app role for the Foundry MI; the tool endpoints expose only the
  two operations, nothing else.
- **Rate limiting + input validation** on the tool endpoints (they're internet-reachable).
- **Guardrails still enforced** server-side: failures are structured errors, no invented rates, no
  mortgage-product recommendation; the not-mortgage-advice framing stays in the agent instructions.
- **Observability**: log tool-endpoint calls with provenance (which caller, latency, success) so we
  can prove the agent-target path works and know when it breaks (the "we know it works, and we'll
  know the moment it stops" bar).

## Testing

- **Offline:** contract/component tests for `/api/agent-tools/*` (request/response shape, guardrail
  error mapping, JWT-middleware rejects unauthenticated/wrong-audience calls via a test handler),
  and an OpenAPI-spec lint (operationId rules, security scheme present). Fast gate.
- **Live `[External]`:** an **agent-target** run — provision the agent with the OpenAPI tool, ask a
  cost question, assert the Foundry service actually called `estimate_mortgage` (tool invoked
  server-side) and the answer contains the estimate. This is the end-to-end proof that unblocks
  reference-by-name.

## Invariant alignment

- **API-first:** the Foundry service calls the HomeScout API's tool endpoints — agent work stays
  behind the API boundary. ✔
- **Not mortgage advice / no product recommendation / no safe-unsafe verdict:** preserved in the
  tool responses + agent instructions; the safety evaluators continue to gate. ✔
- **FluentResults** for expected failures in the tool services (unchanged). ✔
- **Every slice ships tests** (offline contract + live agent-target). ✔
- **Entra managed identity, least-privilege** for the service-to-service call (matches CLAUDE.md);
  end-user auth remains Keycloak. ✔

## Phased steps

1. **Shared agent lib** — extract `AgentDefinition` + `FoundryAgentDeployer` so the API and the CLI
   share it (unblocks startup provisioning + tool declaration).
2. **Tool endpoints** — `/api/agent-tools/estimate-mortgage` + `/base-rate` (thin adapters over the
   existing services) + the OpenAPI spec, with offline contract tests.
3. **Entra app registration + MI role** — audience/app-role; JWT-validation middleware on
   `/api/agent-tools/*`, with offline tests for the middleware.
4. **Declare the OpenAPI tool on the agent definition** — provision with `managed_identity` auth +
   audience; confirm the exact `Azure.AI.Projects.Agents` OpenAPI-tool type at implementation time.
5. **Live agent-target verification** — prove the service calls the tool server-side.
6. **Reference-by-name** (now unblocked) — the API serves the persisted agent; ReasoningOptions take
   effect; graceful fallback if not provisioned.
7. **Optional:** startup check-and-provision fallback behind a flag.

## Open questions / verify-at-implementation

- Exact **.NET declaration type** for an OpenAPI tool in `Azure.AI.Projects.Agents` (the
  `DeclarativeAgentDefinition.Tools` collection is `OpenAI.Responses.ResponseTool` — confirm the
  OpenAPI-tool representation + how managed-identity auth + audience are expressed). Verify against
  Microsoft Learn + the SDK at implementation time.
- **Hosting** for the tool endpoint reachability (public + JWT vs private networking) — decide with
  the deployment target (App Service Easy Auth is a documented, low-effort option).
- Whether to **keep the in-process tools** as a dev/offline fallback (likely yes) alongside the
  server-side tools.

## References

- OpenAPI tools (new Foundry agents): https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/openapi
- Secure OpenAPI tool calls (managed identity, App Service): https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-ai-foundry-openapi-tool
- Foundry agent + tool .NET patterns: [[API-First Foundry Agents]] → Reference Implementations (`AgenticAICore`).
