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

1. **Separate branches + PRs per track.** Frontend branches: `feature/fe-*`; backend
   branches: `feature/be-*` (or `migration/*`). Each is small and independently green.
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

**Frontend (Codex) — build from the design.** Follow
[Codex Frontend Instructions](../02-frontend/codex-frontend-instructions.md). First slice:
design tokens (both themes) + IBM Plex fonts + the app shell (header / sidebar / main /
evidence panel, responsive), then the **mortgage cost estimator end-to-end** against
`/api/mortgage/estimate` + `/api/mortgage/base-rate` (fully API-backed, no Foundry). Branch
`feature/fe-*`; keep `frontend-ci` green.

**Backend (lead) — bring the copilot toward live.** ✅ **Done:** `HomeScoutCopilot.AgentOps`
manifest step ([[GenAIOps Tooling Plan]], Phase 3) — `agentops manifest` generates the
declarative `homescout.agent.yaml` from the single-sourced agent definition, offline +
unit-tested + drift-guarded. The live `CreateAgentVersion` registration is deferred to a
live-verified slice (needs `azd` provision). Ran independently of the frontend's first slice.

**Then (lead):** review Codex's frontend PR **and** the backend PR, merge each individually,
run an end-to-end check (frontend ↔ live mortgage/base-rate endpoints), then plan iteration 2.

**Seam item for iteration 2:** the design's Evidence panel needs structured, tagged,
provenance'd evidence items (`fact`/`estimate`/`assumption`/`missing` + source +
live/cache/fallback). The current `CopilotAnswer.Evidence` is empty — defining that contract
in `Shared` is backend-led seam work, co-designed with Codex's evidence panel.
