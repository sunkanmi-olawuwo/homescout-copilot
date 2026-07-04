# Work Tracks

*Ownership & the API seam for parallel work.*

HomeScout is built by more than one agent at a time. This page defines the **two parallel
tracks**, who owns what, and the **seam** between them, so work can proceed concurrently
without collisions. It exists because the frontend and backend can advance independently as
long as both respect the API contract.

## The seam (the contract between tracks)

The **only** coupling between frontend and backend is the HTTP API:

- **Wire contracts:** `HomeScoutCopilot.Shared` DTOs.
- **Endpoints:** the routes in [[Endpoint Summary]] (`/api/status`,
  `/api/comparison/sample`, `/api/mortgage/estimate`, `/api/mortgage/base-rate`,
  `/api/copilot/ask`).
- **Typed client:** `HomeScoutCopilot.API.Client` (`HomeScoutApiClient`).

**Seam-first rule (from `AGENTS.md`):** a new capability's *shape* (endpoint + DTOs) is
defined and merged **before** either side builds against it. The backend owns and publishes
the contract; the frontend codes to it. If the frontend needs something new, it requests the
contract — it does not invent an endpoint or reach past the API.

## Track A — Frontend (typically another agent)

- **Owns:** `frontend/` only.
- **Plan:** [Frontend Implementation Plan](../02-frontend/frontend-implementation-plan.md)
  → [Design Brief](../02-frontend/design-brief.md) → `wiki/frontend-design-guidelines.md`
  (binding).
- **Does:** review the design, build the design system → screens → the copilot conversation
  surface, calling the API via `HomeScoutApiClient`. Component (Vitest) + E2E (Playwright) +
  a11y tests.
- **Must not:** edit `dotnet/` service/agent code, import an agent/LLM SDK (API-first
  invariant — the drift check enforces this), or change `Shared` DTOs unilaterally.
- **CI it must keep green:** `frontend-ci` (build + lint + unit + e2e).

## Track B — Backend / GenAIOps (this reviewer)

- **Owns:** `dotnet/` — `src/` (API, API.Service, API.Client, Shared, Functional,
  AppHost, ServiceDefaults), the new `tools/` ([[GenAIOps Tooling Plan]]:
  `AgentOps` + `Evaluator`), and `tests/`. Also `infra/`.
- **Plan:** [03-backend](../03-backend/README.md) — [Copilot Agent Gateway](../03-backend/copilot-agent-gateway-plan.md),
  [GenAIOps Tooling](../03-backend/genaiops-tooling-plan.md),
  [Mortgage Cost Estimator](../03-backend/cost-estimator-mortgage-plan.md),
  [API Vertical Slices](../03-backend/api-vertical-slice-plan.md).
- **Does:** keep the API contract stable + documented; build the persisted-agent deploy
  tool, the eval harness, retrieval, and data integrations behind the API boundary.
- **Must not:** edit `frontend/` implementation. When a DTO/endpoint must change,
  update `Shared` + [[Endpoint Summary]] + the typed client **and announce it** (the seam
  changed) so the frontend track can adapt.
- **CI it must keep green:** `backend-ci`, `plan-drift`, CodeQL.

## Rules that keep parallel work clean

1. **Each agent works on its own branch — never on `main`.** `main` is protected and
   changes only via a reviewed, gated PR (per `AGENTS.md`). Frontend branches: `feature/fe-*`;
   backend branches: `feature/be-*` (or `migration/*`). Cut every branch from up-to-date
   `main`, keep it small + independently green, and never commit directly to `main` or push to
   another track's branch.
2. **No shared files across tracks** except `Shared` DTOs and the endpoint docs — and those
   are backend-owned; frontend consumes.
3. **Contract changes are backend-led and announced.** A frontend need becomes a backend
   contract PR first (seam-first), then the frontend PR builds on it.
4. **Both run the full quality gate** (`scripts/quality-gate.sh`) locally; both sets of CI
   checks must be green to merge.
5. **Plan-sync per `AGENTS.md`:** each track updates its owning plan + [[Log]] with its work;
   keep route/DTO/tool names identical between code and the owning plan.

## Iteration 1 plan (2026-07-04)

The design is finished — the interactive prototype is `wiki/raw/HomeScout Copilot.html`
(source of truth in Claude Design). The seam is live: status, comparison-sample, mortgage
estimate + base-rate, and copilot-ask endpoints exist with the typed client + `Shared` DTOs
(copilot returns 503 until Foundry is provisioned).

**Frontend (Codex) — build from the design.** ✅ **Done + lead-reviewed:** design tokens
(both themes) + IBM Plex + the responsive three-region shell; the **copilot conversation as
the main surface** (graceful 503) and the **mortgage estimator as the right-rail panel**,
wired to `/api/mortgage/estimate` + `/api/mortgage/base-rate`, matching the design's IA. Lead
reworked it to the design's layout (was leading with the estimator) and fixed the segmented
control + a metric-row flexbox collapse; verified visually + live end-to-end. Follows the
[Codex Frontend Instructions](../02-frontend/codex-frontend-instructions.md).

**Backend (lead) — bring the copilot toward live.** ✅ **Done:** `HomeScoutCopilot.AgentOps`
manifest step ([[GenAIOps Tooling Plan]], Phase 3) — `agentops manifest` generates the
declarative `homescout.agent.yaml` from the single-sourced agent definition, offline +
unit-tested + drift-guarded. The live `CreateAgentVersion` registration is deferred to a
live-verified slice (needs `azd` provision). Ran independently of the frontend's first slice.

**Then (lead):** review Codex's frontend PR **and** the backend PR, merge each individually,
run an end-to-end check (frontend ↔ live mortgage/base-rate endpoints), then plan iteration 2.

**Iteration 1 outcome (2026-07-04):** both merged (#32 backend AgentOps manifest, #33 frontend
workspace + estimator). Frontend reworked to the design IA and verified live end-to-end.

## Iteration 2 plan (2026-07-04)

**Theme: light up the copilot — structured evidence + live Foundry.** Turn `/api/copilot/ask`
from a 503 stub into a real, grounded answer with a provenance-tagged evidence trail rendered
in the design's Evidence panel.

**Seam-first ordering (there is a dependency this time).** The frontend's Evidence panel +
answer rendering build against a contract the backend must define **first**. So:

1. ✅ **Backend seam PR — the evidence contract (done + live-verified 2026-07-04).**
   `FigureKind` + `EvidenceItem` + `CopilotAnswer.Evidence` in `Shared/Contracts`;
   `CopilotEvidenceBuilder` maps tool results → tagged evidence (estimate → payment / LTV as
   `Estimate`; base rate → `Fact` + provenance), wired into the gateway. Enums travel as
   lowercase strings on the wire; the typed client + tests read them via `JsonStringEnumConverter`.
   Offline unit + endpoint tests, plus the live agent test asserting real evidence. **Codex is
   unblocked** — see [Codex Frontend Instructions](../02-frontend/codex-frontend-instructions.md)
   "Second slice".
2. ✅ **Foundry provisioning (done + live-verified 2026-07-04).** `azd provision` into
   subscription **HomeScoutPilot** (eastus2): account + `gpt-5-mini` `chat` deployment + project
   + RBAC. Fixed three real deployability issues found live (deprecated model → gpt-5-mini,
   GlobalStandard SKU, `allowProjectManagement`). `/api/copilot/ask` is **live** with the
   `AZURE_FOUNDRY_*` env set; `FoundryAgentGatewayLiveTests` green. See [infra README](../../../infra/README.md).

## Next set of work (active)

**Frontend (Codex) — the copilot second slice.** ✅ **Implemented on
`feature/fe-copilot-evidence` (PR/review pending):** the composer and START WITH cards now post
to `POST /api/copilot/ask`; successful answers render `text`, `toolCalls`, assumptions and
caveats in the conversation; the right-rail Evidence tab hydrates from `answer.evidence`
using the lowercase `kind` chip, Live/Cache/Fallback provenance badge and source. The graceful
503 fallback remains for environments without Foundry configuration. Verified with mocked
contract responses in component + Playwright E2E tests.

**Backend (lead) — active + queued:**
- ✅ **Evaluator harness — done, live-verified** ([[GenAIOps Tooling Plan]]):
  `HomeScoutCopilot.Evaluator` covers all three verbs. `evaluator safety` runs deterministic
  HomeScout guardrail evaluators (not-mortgage-advice, no product recommendation, no
  safe/unsafe area verdict) over a version-controlled dataset (offline). `evaluator run` runs
  the same evaluators over the **live** copilot's real answers (external, 6/6). `evaluator
  quality` adds **model-graded quality** — an **LLM judge** scores each live answer on
  relevance / usefulness / groundedness (1–5, pass ≥ 3) with a rationale; rubric + parsing are
  pure/offline-tested, the model call is `[Category("External")]` (`QualityLiveTests`).
  Live-verified 2026-07-04: 6/6, all dims avg 5.0 against the provisioned agent.
  On top of the bespoke verb, a **standard-library harness** (`HomeScoutCopilot.Evaluation.Test`)
  runs the first-party **`Microsoft.Extensions.AI.Evaluation`** evaluators (`Relevance`/`Coherence`/
  `Fluency`) **and** the bespoke judge **and** the guardrails **and** opt-in Foundry content-safety
  (`Hate`/`Violence`/`SelfHarm`/`Sexual`) in one origin-labelled report — results persisted to a
  keyless **Azure ADLS Gen2** store (`infra/modules/eval-storage.bicep`) for regression history +
  `dotnet aieval` reports (`scripts/eval-report.sh`). All live-verified 2026-07-04. Only the Foundry
  *portal* evaluation runs remain optional — see [[Plan Divergence]].
- ✅ **AgentOps `CreateAgentVersion` — done, live-verified** ([[GenAIOps Tooling Plan]]):
  `agentops deploy` registers the agent as a persisted, versioned Foundry agent
  (`AgentAdministrationClient.CreateAgentVersionAsync`, `Azure.AI.Projects.Agents` 2.0.0 GA) so it
  shows in the portal as a named, versioned asset. Idempotent on identical content. Verified live
  2026-07-04 (`FoundryAgentDeployerLiveTests`). Tools stay client-side, so cloud eval uses
  BYO-responses (not agent-target).
- ✅ **In-process reasoning tuning (done, live-verified)** — the runtime copilot sets
  `ChatOptions.Reasoning = { Effort = Medium }` on each run (portable Microsoft.Extensions.AI
  surface), the correct knob for our gpt-5 reasoning model. Verified 2026-07-04: still calls
  `estimate_mortgage`, no error. This gives the one runtime gain reference-by-name would have,
  without its cost — following AgenticAICore's in-process ("Local") pattern.
- **Deferred by decision (2026-07-04):**
  - **Server-side tools** ([[Server-Side Tools Plan]]) — OpenAPI tools on the persisted agent.
    Deferred: agent-target eval is covered better by BYO-responses, in-process tools are fully
    local-testable + secure, and it would add a public inbound surface. Design stays ready.
  - **Reference-by-name** — blocked on server-side tools (a live spike proved serving the persisted,
    tool-less agent breaks tool-calling; reverted). Not needed once in-process reasoning is set.
  - **Persisted Foundry agent** (`agentops deploy`) is now a **decoupled, optional GenAIOps/portal
    asset** (versioned agent registry + portal visibility), not on the runtime path — deploy in CI
    on prompt changes; the app runs in-process from the same `AgentPrompt` source.
- **Queued next (planned):**
  0. ✅ **Foundry portal cloud eval (BYO-responses) — done, live-verified** ([[GenAIOps Tooling Plan]]):
     `evaluator answers --out` writes the live copilot's real `{query,response}`; the isolated
     `HomeScoutCopilot.PortalEval` tool publishes an evaluation run to the portal via the OpenAI
     Evals API (`score_model` graders — Azure's `azure_ai_evaluator`/`builtin.*` aren't accepted).
     `scripts/portal-eval.sh` runs both. Verified 2026-07-04 (4 passed / 2 failed / 0 errored).
  0b. ✅ **Expanded eval dataset (done)** — `homescout-eval.jsonl` grown from 6 → **30** curated
     cases: capability rows (LTV/term/rate variants, overpayment, deposit, stamp duty, leasehold,
     base-rate-vs-offered, area schools/commute/EPC context, affordability, remortgage) + **9
     adversarial guardrail probes** (`probe-*`: which-mortgage, best-deal, fix-or-track, should-i-buy,
     is-X-safe, safest-area, will-rates-drop, which-lender, remortgage-now). All 30 golden responses
     pass the offline safety evaluators; the probes exercise the live guardrails under pressure.
  1. **Multi-turn conversation threads (anonymous)** ([[Conversation Threads Plan]]) —
     `AgentThread`-based conversation memory keyed by an **anonymous session id** (no auth needed),
     so follow-ups keep context; in-memory first, durable (Cosmos / Standard setup) next. **No query
     rewrite** — the model resolves follow-ups from the full history the framework passes; rewrite
     only matters once RAG lands. Add **multi-turn eval cases** to prove context carries (the
     `cost-interest-only` follow-up is the canonical test, currently evaluated standalone). End-user
     auth (**Keycloak**, see [[Plan Divergence]]) and per-user history follow later.
     **Parallel split:** backend (lead) — HttpOnly `hs_session` cookie + `AgentThread` registry +
     `POST /api/copilot/session/reset` (60-min idle / 24-h cap); frontend (Codex) — a "New
     conversation" button against that contract (the cookie is automatic, so no other FE change).
     ✅ **Frontend half implemented on `feature/fe-conversation-reset`**: compact-header reset
     control posts to the reset endpoint and clears the visible thread/evidence state.
  4. **Foundry portal cloud eval (BYO-responses)** — publish `{query, real answer}` runs to the
     portal via the `EvaluationClient` for portal charts + run comparison, over an expanded (~30)
     curated dataset.
  5. **Area-comparison endpoint** — product breadth (the design's Greenwich/Croydon screen:
     commute/crime/EPC/schools); needs public-data sources, edging into Phase 6.

**Then (lead):** review Codex's frontend + the backend slice, merge individually, E2E check
(copilot conversation ↔ live `/api/copilot/ask`), then plan iteration 3.

## Copilot answer readability (in flight)

Two coordinated fixes so the answer reads well and the conversation view behaves like a chat:

- ✅ **Backend (lead) — prompt v2 (done, live-verified):** `homescout.v2.md` (bump
  `AgentPrompt.Version` → `v2`) instructs the agent to answer in **structured Markdown**
  (bold headline → `##` sub-headings + bullets), and to lean on the evidence panel for figures
  rather than re-listing them. Guardrails intact; `evaluator run` stayed 6/6 against the live
  agent. Manifest regenerated to `homescout.v2.md`.
- **Frontend (Codex):** (1) **collapse the hero** (big "Compare areas and properties…" H1 +
  START WITH cards) into a compact header once a conversation is active — empty state only
  before the first question; (2) **render `answer.text` as sanitized Markdown** (headings /
  bullets / bold). See [Codex Frontend Instructions](../02-frontend/codex-frontend-instructions.md)
  "Third slice".
