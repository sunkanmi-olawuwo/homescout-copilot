# Readiness Checklist

This page is the starting gate for HomeScout implementation sessions. Updated 2026-07-04.

## Current State

- Repository is on `main`; origin `https://github.com/sunkanmi-olawuwo/homescout-copilot.git`.
- The RagLab skeleton migration is complete: `dotnet/src` (AppHost, ServiceDefaults, **API**
  host, **API.Service** application layer, API.Client, Shared, Functional) + `dotnet/tests`;
  React frontend at `frontend/` (pnpm). The frontend-facing API is `HomeScoutCopilot.API`
  (the Aspire resource is named `apiservice`).
- **Shipped:** the mortgage cost estimator + BoE base-rate provider (live, provenance-tagged);
  the copilot boundary `IHomeScoutAgentGateway` + `FoundryAgentGateway` (Microsoft Agent
  Framework, keyless `DefaultAzureCredential`) + `HomeScoutAgentTools`; the versioned agent
  prompt asset (`Prompts/homescout.v1.md` via `AgentPrompt`); Carter + MediatR vertical
  slices with validated options; `infra/` (azd + bicep, Basic Foundry setup).
- Endpoints live: `/api/status`, `/api/comparison/sample`, `/api/mortgage/estimate`,
  `/api/mortgage/base-rate`, `/api/copilot/ask` (503 until Foundry is provisioned).
- Microsoft Foundry Agent Service is the target platform. GenAIOps tooling is **total .NET**
  (no Python) — confirmed by SDK spike; see [[Plan Divergence]].
- `wiki/` is the canonical project memory.

## Plans In Sync

- [[Work Tracks]] — the two parallel tracks (frontend / backend) and the API seam between them.
- [[Phased Learning And Build Plan]] — the canonical day-to-day sequence.
- [[GenAIOps Reference Implementation]] — adoptable patterns from `mslearn-genaiops`.
- [[GenAIOps Tooling Plan]] — the `AgentOps` + `Evaluator` tool projects (backend next step).
- [[Frontend Implementation Plan]] — review the design, then build (frontend track).
- [[GenAIOps Learning Path Integration]] — Microsoft Learn modules → prompt governance,
  evaluations, monitoring, tracing.
- [[API-First Foundry Agents]] — the backend boundary and Foundry direction.
- [[Frontend Design Guidelines]] — binding for UI work.
- [[Testing Strategy]] — unit, API, integration, UI, retrieval, and AI-evaluation expectations.

## Start Here (by track)

Pick the track you own — see [[Work Tracks]] for the boundaries and the shared API seam.

**Backend / GenAIOps track:**

1. Build [[GenAIOps Tooling Plan]] Phase 3: `HomeScoutCopilot.AgentOps` — register the
   versioned Foundry agent from the prompt asset/manifest (`CreateAgentVersion`).
2. Then `HomeScoutCopilot.Evaluator` + a first hand-curated eval set (built-in + HomeScout
   safety evaluators).
3. Keep the API contract + [[Endpoint Summary]] stable; announce any DTO/endpoint change.

**Frontend track:**

1. [[Frontend Implementation Plan]] Stage 1 — review the design brief + design-agent
   deliverables; extract design tokens to code.
2. Stage 2 — design system → screens → copilot conversation surface, against the live API
   via `HomeScoutApiClient`.

## Not Yet Started

- Persisted, versioned Foundry agent deploy (`AgentOps`) + the eval harness (`Evaluator`).
- AI evaluation dataset + rubric (safety evaluators).
- Full frontend design implementation (design system → screens → copilot surface).
- Curated knowledge-base folder + user-owned case-file storage + retrieval (Phase 6).
- Streaming + tracing/monitoring (Phases 4/7).
- `azd provision` live-verification of the Foundry slices; CI Azure OIDC.

## Ready Criteria

We are ready to start implementation when:

- The working tree is clean or only contains intentional current-session changes.
- The track and next step are clear from [[Work Tracks]] + the owning plan.
- Any course/plan divergence is recorded in [[Plan Divergence]].
- Significant code work will be paired with wiki updates and tests, on its own branch → PR →
  gated merge.
