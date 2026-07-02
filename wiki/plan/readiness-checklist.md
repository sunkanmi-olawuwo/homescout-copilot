# Readiness Checklist

This page is the starting gate for HomeScout implementation sessions.

## Current State

- Repository is on `main`.
- GitHub origin is `https://github.com/sunkanmi-olawuwo/homescout-copilot.git`.
- React is the frontend from the start.
- `HomeScoutCopilot.ApiService` is the frontend-facing API.
- Microsoft Foundry Agent Service is the target agent platform.
- Current Microsoft Foundry Agent Service SDK/API packages are the intended implementation path for real agent work, but the exact package/API route must be verified before dependencies are added.
- The course companion repo remains reference material for concepts, sequencing, and standard patterns, not the implementation shape.
- `wiki/` is the canonical project memory.

## Plans In Sync

- [[Phased Learning And Build Plan]] is the canonical day-to-day sequence.
- [[Video Implementation Roadmap]] maps available playlist videos and companion commits.
- [[GenAIOps Learning Path Integration]] maps Microsoft Learn modules into prompt governance, evaluations, monitoring, and tracing.
- [[RAG Architecture]] defines user-owned case files and the curated HomeScout knowledge base.
- [[API-First Foundry Agents]] defines the backend boundary and Foundry agent direction.
- [[Frontend Design Guidelines]] is binding for UI work.
- [[Testing Strategy]] defines unit, API, integration, UI, retrieval, and AI evaluation expectations.

## Start Here

Next implementation session:

1. Work Phase 1 from [[Phased Learning And Build Plan]].
2. Watch or review playlist Parts 1-2 with the current repo open.
3. Keep React as the product UI and translate course Blazor behavior into React/API boundaries.
4. Verify the current workspace shell against [[Frontend Design Guidelines]].
5. Build the first real Phase 1 increment: a polished comparison workspace shell with API status integration and placeholder states that match HomeScout language.
6. Run frontend build, API build, and the current test project.
7. Update [[Feature Coverage]], [[Component Architecture]], and [[Log]].

## Not Yet Started

- Real `POST /api/comparisons/draft` endpoint.
- Deterministic `estimate_monthly_costs` tool.
- Prompt inventory.
- Foundry agent gateway abstraction.
- Current Foundry Agent Service SDK/API route selection.
- Curated knowledge-base folder.
- User-owned case-file storage.
- AI evaluation dataset and rubric.
- Azure deployment plan.

## Ready Criteria

We are ready to start implementation when:

- The working tree is clean or only contains intentional current-session changes.
- The next phase is clear from [[Phased Learning And Build Plan]].
- The relevant video note has been read before implementation.
- Any course divergence is recorded in [[Plan Divergence]].
- Significant code work will be paired with wiki updates and tests.
