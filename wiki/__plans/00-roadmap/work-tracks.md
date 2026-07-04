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

**Frontend (Codex) — the copilot second slice.** Now unblocked (evidence contract merged,
copilot live). Follow [Codex Frontend Instructions](../02-frontend/codex-frontend-instructions.md)
"Second slice":
- **Conversation answers** — send the composer + START WITH cards to `POST /api/copilot/ask`;
  render `text` + tool chips; keep the graceful 503 fallback for envs without Foundry.
- **Evidence panel** — populate the right-rail Evidence tab from `answer.evidence` (the
  lowercase `kind` chip + `provenance` badge + source), per the design. Build against the merged
  contract with mocks; works live wherever `AZURE_FOUNDRY_*` is set. Branch `feature/fe-*`.

**Backend (lead) — active + queued:**
- ✅ **Evaluator harness — safety evals done** ([[GenAIOps Tooling Plan]]):
  `HomeScoutCopilot.Evaluator` (`evaluator safety`) runs deterministic HomeScout guardrail
  evaluators (not-mortgage-advice, no product recommendation, no safe/unsafe area verdict) over
  a version-controlled eval dataset, scored + summarised, drift-guarded. The **Foundry cloud
  evals** (model-graded intent/relevance/groundedness over live copilot responses) follow as
  the next live-verified step (seam-first: offline proven, external adapter verified live).
- **Queued next (planned):**
  1. **AgentOps `CreateAgentVersion`** — the deferred live-deploy: register the versioned agent
     server-side (portal versions + reference-by-name) now that Foundry is up.
  2. **Area-comparison endpoint** — product breadth (the design's Greenwich/Croydon screen:
     commute/crime/EPC/schools); needs public-data sources, edging into Phase 6.

**Then (lead):** review Codex's frontend + the backend slice, merge individually, E2E check
(copilot conversation ↔ live `/api/copilot/ask`), then plan iteration 3.
