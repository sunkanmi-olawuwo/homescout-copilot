# HomeScout Plans

## Purpose

`wiki/__plans/` is the living implementation map for HomeScout Copilot. It
separates sequencing (roadmap), product/design rules, frontend work, backend work,
and testing so implementation moves in the right order without burying
dependencies inside individual feature plans.

This structure mirrors the RagLab (`HBK.Insights.Raglab`) skeleton. The active
restructuring effort is tracked in the master migration plan below.

Start here:

- [HomeScout Skeleton Migration — Master Sequenced Plan](./00-roadmap/homescout-skeleton-migration-plan.md)

## Folder Structure

### 00-roadmap

Roadmap and sequencing plans — what happens first, what can wait, which supporting
plans own the detail.

- [HomeScout Skeleton Migration — Master Sequenced Plan](./00-roadmap/homescout-skeleton-migration-plan.md)
- [Phased Learning And Build Plan](./00-roadmap/phased-learning-build-plan.md)
- [Work Tracks & Ownership](./00-roadmap/work-tracks.md) — the two parallel tracks (frontend / backend) and the API seam between them, for concurrent work.
- [Readiness Checklist](./00-roadmap/readiness-checklist.md)
- [Video Implementation Roadmap](./00-roadmap/video-implementation-roadmap.md)
- [Release Monitoring](./00-roadmap/release-monitoring.md)
- [Plan Divergence](./00-roadmap/plan-divergence.md)

#### 00-roadmap/course

Course-learning material: the playlist-to-product mapping and per-video notes.
Reference and sequencing scaffolding, not product feature plans.

- [Course Playlist Tracker](./00-roadmap/course/course-playlist-tracker.md)
- Video notes:
  [00](./00-roadmap/course/video-notes/00-series-intro.md) ·
  [01](./00-roadmap/course/video-notes/01-repo-aspire-solution.md) ·
  [02](./00-roadmap/course/video-notes/02-blazor-server-baseline.md) ·
  [03](./00-roadmap/course/video-notes/03-tool-calls.md) ·
  [04](./00-roadmap/course/video-notes/04-reasoning.md) ·
  [05](./00-roadmap/course/video-notes/05-streaming.md) ·
  [06](./00-roadmap/course/video-notes/06-conversations.md) ·
  [07](./00-roadmap/course/video-notes/07-image-generation.md) ·
  [08](./00-roadmap/course/video-notes/08-image-pdf-input.md) ·
  [09](./00-roadmap/course/video-notes/09-user-auth.md) ·
  [10](./00-roadmap/course/video-notes/10-user-memory-personalization.md) ·
  [11](./00-roadmap/course/video-notes/11-speech-input.md)

### 01-design

Product rules, UX decisions, domain shape, and design-level plans.

- [Product Brief](./01-design/product-brief.md)
- [GenAIOps Learning Path Integration](./01-design/genaiops-learning-path.md)
- [GenAIOps Reference Implementation (mslearn-genaiops)](./01-design/genaiops-reference-implementation.md) — concrete, adoptable patterns from Microsoft's official GenAIOps lab repo, mapped to HomeScout phases.

### 02-frontend

React frontend implementation plans and UI wiring guidance.

- [HomeScout Copilot — Design Brief](./02-frontend/design-brief.md) — the full,
  design-agent-ready specification (vision, IA, all screens/flows, copilot UX, design
  system, states, accessibility, deliverables).
- [Frontend Implementation Plan — Review The Design, Then Build](./02-frontend/frontend-implementation-plan.md) — the frontend build phase: review the design, then implement the React app against the API.
- [Codex Frontend Instructions — Build From The Claude Design](./02-frontend/codex-frontend-instructions.md) — handoff for Codex: working from the design HTML, tokens, API seam, guardrails, first slice.
- [Frontend Plans (overview)](./02-frontend/README.md)

### 03-backend

API, service-layer, persistence, and backend integration plans.

- [API Vertical Slices + Validated Options — Plan (RagLab parity)](./03-backend/api-vertical-slice-plan.md)
- [Copilot Agent Gateway — Design (Foundry Agent Service)](./03-backend/copilot-agent-gateway-plan.md)
- [GenAIOps Tooling — AgentOps + Evaluator Projects](./03-backend/genaiops-tooling-plan.md) — the two .NET tool projects for deploying versioned agents and running evaluations.
- [Server-Side Tools — OpenAPI Tools on the Foundry Agent](./03-backend/server-side-tools-plan.md) — design-first plan (managed-identity auth, endpoint contract, egress, startup provisioning); prerequisite for reference-by-name.
- [Conversation Threads — Multi-Turn, Anonymous](./03-backend/conversation-threads-plan.md) — design-first plan (AgentThread per anonymous session; no query rewrite; multi-turn eval cases; durable store + Keycloak later).
- [Mortgage Cost Estimator — Design (MVP)](./03-backend/cost-estimator-mortgage-plan.md)
- [Rental Cost Estimator — Design (renter analogue of the mortgage estimator)](./03-backend/rental-cost-estimator-plan.md) — deterministic true-monthly + upfront/deposit cost for renters (Tenant Fees Act 2019 caps).
- [Keycloak Auth + Per-User History — Design](./03-backend/keycloak-auth-plan.md) — design-first plan (Aspire-hosted Keycloak; JWT bearer; `(Provider, Subject)` → internal user id; per-user session history; anonymous→authenticated hand-off), modelled on RagLab.
- [Backend Plans (overview)](./03-backend/README.md)

### 04-testing

Cross-cutting QA, quality-gate, and end-to-end test plans.

- [Quality Gate & Test Plan](./04-testing/quality-gate-plan.md)
- [Testing Plans (overview)](./04-testing/README.md)

## How To Use These Plans

- Build in phase order. Do not start a slice before its roadmap dependency exists.
- Keep entity/route/field names identical between code and the owning plan. If one
  needs a name the other lacks, change both in the same commit.
- Update this README whenever a plan file is added, renamed, or removed so the
  index stays complete.
- Cross-reference wiki pages with `[[Page Name]]` links; use relative Markdown
  links (like this file) when pointing at exact plan files.

## Detecting Plan Drift

The plans and the `dotnet/` code must not drift apart. From Phase 1 this is
enforced mechanically by `scripts/check-plan-drift.sh` (run locally and in
`.github/workflows/plan-drift.yml`). It reports two severities:

- **FAIL** — an invariant that must never be violated; blocks CI.
- **WARN** — forward-looking convergence expected to be pending until its phase
  lands; reported but non-blocking.

Invariants the drift check enforces (see the master migration plan for the full
list):

- **API-first** — the React frontend calls the API; it never owns agent
  orchestration.
- **Microsoft Foundry Agent Service** is the target agent platform; no classic
  Foundry agents for new work.
- **Not mortgage advice** — no regulated mortgage-product recommendation, no
  simplistic safe/unsafe area label, estimates always ship with assumptions.
- **Expected failures use FluentResults**, not exceptions, in the service layer.
- **Every plan file is indexed in this README**, and README links resolve.
- **Every slice ships tests** for the surfaces it changes.

Until the script exists (Phase 1), check drift manually: confirm this index is
complete, that relative links resolve, and that any code/plan name changes were
made on both sides in the same change.
