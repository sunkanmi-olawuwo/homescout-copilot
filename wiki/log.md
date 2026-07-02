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
