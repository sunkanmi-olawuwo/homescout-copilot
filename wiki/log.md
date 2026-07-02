# Log

## 2026-07-02

### Repository Created

- Created HomeScout Copilot as a .NET Aspire + Blazor solution.
- Added `AppHost`, `ServiceDefaults`, `Web`, `ApiService`, and `Tests` projects.
- Added an initial HomeScout-branded Blazor workspace page.
- Added `NuGet.Config`.
- Pinned `Microsoft.OpenApi` to avoid the vulnerable transitive `2.0.0` package.
- Verified `dotnet restore`.
- Verified NuGet vulnerability audit returned no vulnerable packages.
- Full solution build was attempted but the build process became stuck in the sandbox with no output and could not be terminated due restricted process controls.

### Repository Moved

- Moved the repository to `/Users/olaheavy/source/code/rag/home-scout-pilot`.
- Verified Git history and clean working tree after the move.

### Wiki Structure Created

- Added canonical wiki structure under `wiki/`.
- Migrated existing product brief, playlist tracker, and video notes into `wiki/plan/`.
- Added `AGENTS.md` and planned `CLAUDE.md` symlink so assistant rules are shared.
- Added plan divergence workflow so future plan/tool comparisons use `wiki/plan/` as the canonical source.

### Removed Legacy Docs Compatibility

- Removed the `docs/` compatibility symlink layer.
- Kept `wiki/` as the only documentation home.
- Updated README, agent rules, plan overview, and plan divergence notes so plan checks read `wiki/plan/` directly.

### Planned Available Course Videos

- Refreshed the YouTube playlist feed and confirmed 12 available entries: intro plus Parts 1-11.
- Updated the companion repo clone and confirmed latest state `062d953 Part 11 Done`.
- Added [[Video Implementation Roadmap]].
- Expanded [[Course Playlist Tracker]] with companion-code references.
- Added per-video notes for Parts 2-11 under `wiki/plan/video-notes/`.


### Added Release Monitoring Routine

- Added [[Release Monitoring]] with commands for checking playlist RSS updates and companion repo commits.
- Linked the routine from the wiki index, plan overview, and course playlist tracker.

### Adopted API-First Foundry Agent Direction

- Fact-checked Microsoft Foundry Agent Service direction against current Microsoft Learn docs.
- Added [[API-First Foundry Agents]].
- Updated component architecture, endpoint summary, roadmap, tracker, and plan divergence to make HomeScout API-first and Foundry-agent oriented.

### Pivoted HomeScout To React From Part 1

- Removed the Blazor frontend project from the implementation direction.
- Added a React/Vite frontend as the product frontend path from the first course implementation part.
- Updated plan divergence and video notes so course Blazor code is treated as reference material only.

### Replaced Blazor Frontend With React

- Removed `HomeScoutCopilot.Web` from the implementation.
- Added `frontend/` as a Vite React project.
- Updated `AppHost` to host the React frontend with `AddViteApp`.
- Added `/api/status` and `/api/comparison/sample` endpoints to `HomeScoutCopilot.ApiService`.
- Verified restore, npm install/audit/build, API/AppHost builds, and Aspire integration test.

### Added GenAIOps Learning Path Integration

- Added [[GenAIOps Learning Path Integration]] based on Microsoft's Operationalize generative AI applications learning path.
- Mapped each learning-path module to HomeScout artifacts: architecture records, prompt governance, evaluations, CI automation, monitoring, and distributed tracing.
- Defined how future video notes should include a GenAIOps hook when the feature touches prompts, agents, tools, retrieval, evaluation, monitoring, or tracing.

### Added Phased Learning And Build Plan

- Added [[Phased Learning And Build Plan]] to sequence videos, Microsoft Learn GenAIOps modules, product design, API-first implementation, testing, evaluations, and Azure deployment management.
- Moved product design into the early phases before deep AI behavior.
- Added explicit expectations for AI evaluations in [[Testing Strategy]].

### Verified GenAIOps Modules Individually

- Opened each of the six Microsoft Learn GenAIOps modules and recorded the checked unit lists in [[GenAIOps Learning Path Integration]].
- Adjusted [[Phased Learning And Build Plan]] so full tracing work follows the monitoring/deployment foundation, while lightweight correlation ids can still be introduced earlier.

### Added RAG Architecture To Plan

- Added [[RAG Architecture]] to define HomeScout's two retrieval layers: user-owned case files and the curated HomeScout knowledge base.
- Updated [[Phased Learning And Build Plan]] so curated knowledge starts early as source-controlled assumptions and safety notes, while case-file retrieval arrives with saved comparisons, uploads, and user scoping.
- Updated architecture, feature coverage, and testing notes for case-file retrieval and curated knowledge-base retrieval.

### Added Curated Knowledge Source Strategy

- Updated [[RAG Architecture]] with source rules for the curated HomeScout knowledge base.
- Clarified that HomeScout should store short authored notes with source metadata, not raw scraped external websites.
- Listed seed source families including GOV.UK, HM Land Registry, RICS, Police.uk, OpenStreetMap/Overpass, official school datasets, and internal HomeScout safety rules.

### Reviewed Plans For Start Readiness

- Reviewed the phased plan, video roadmap, GenAIOps plan, RAG architecture, product brief, API-first architecture, endpoint summary, feature coverage, testing strategy, and plan divergence notes.
- Updated stale starter references so docs match the current React/API state.
- Added [[Readiness Checklist]] as the starting gate for implementation sessions.

### Clarified Foundry SDK Direction

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] to explicitly say HomeScout will use the new Microsoft Foundry Agent Service SDK/API surface for real agent work.
- Clarified that the course companion repo is a guide for concepts, sequencing, and standard implementation patterns, not the target architecture or SDK surface.
- Updated [[Endpoint Summary]] to describe future tools as Foundry Agent Service tools or backend wrappers rather than course-specific Agent Framework tools.

### Softened Foundry Package Assumption

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] so `Azure.AI.Agents.Persistent` is treated as a candidate package to re-check, not a locked dependency.
- Clarified that the first Foundry implementation should prefer the new Foundry project endpoint and Responses API path unless current implementation-time docs indicate a better SDK route.

### Aligned Foundry SDK Plan To Microsoft SDK Overview

- Updated [[API-First Foundry Agents]], [[Overview]], [[Readiness Checklist]], and [[Plan Divergence]] to use Microsoft's Foundry SDK overview as the anchor.
- Clarified that HomeScout should use the OpenAI SDK against Foundry `/openai/v1` for direct model calls, and the Foundry SDK for project, agent, index, evaluation, and tracing platform work.
- Removed `Azure.AI.Agents.Persistent` from the planned default path.
