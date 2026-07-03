# HomeScout Copilot

HomeScout Copilot is a homebuying due-diligence assistant (property and area
comparison), **not** a regulated mortgage adviser. It is built phase-by-phase
against a living plan in [`wiki/__plans/`](wiki/__plans/). The master sequence is
the source of truth:
[`wiki/__plans/00-roadmap/homescout-skeleton-migration-plan.md`](wiki/__plans/00-roadmap/homescout-skeleton-migration-plan.md).

Backend: .NET Aspire (AppHost + ServiceDefaults + API). Frontend: React + Vite +
**pnpm** at `frontend/`. Target agent platform: **Microsoft Foundry Agent Service**. Expected
domain/application failures use **FluentResults**, not exceptions. `wiki/` is the
canonical development memory; `CLAUDE.md` is a symlink to this file so Claude,
Codex, Copilot, and other agents follow the same rules.

## Engineering Standards

HomeScout is built as production-grade software, to Microsoft's standards — not
tutorial or throwaway code.

- **Production-grade by default.** Correct error handling, tests, observability,
  security, and maintainability are part of every change, not afterthoughts. No dead
  code, no "temporary" hacks on `main`, no tutorial scaffolding shipped as product.
- **Follow Microsoft's official guidance for the stack:**
  - .NET Framework Design Guidelines and the C#/.NET coding conventions.
  - ASP.NET Core / minimal APIs, .NET Aspire, and Microsoft Foundry Agent Service
    guidance and samples.
  - Azure Well-Architected Framework (reliability, security, cost optimization,
    operational excellence, performance efficiency) for cloud and deployment work.
  - Microsoft Entra identity with managed identity and least-privilege access.
- **Use the current, documented, non-deprecated API surface.** Verify against
  Microsoft Learn at implementation time rather than relying on memory; pin package
  versions deliberately.
- **Security, accessibility, and observability are first-class**, not optional extras.
- **Verify, don't assume — especially external dependencies.** Never ship an
  integration you have not seen succeed; a "live" call nobody has watched work is
  just a hidden fallback. Prove it end-to-end with a live test, keep that test out
  of the blocking gate (a third-party outage must not block merges — tag it
  `[Category("External")]` and run it on a schedule), degrade gracefully on failure,
  and make production report which path served the value (e.g. `Live`/`Cache`/
  `Fallback` provenance + a log on fallback). The bar is: *we know it works, and
  we'll know the moment it stops* — not *we hope it works*.

## Plan Sync Protocol

The plans and the code must not drift apart. Whenever you implement, change, or
review product behavior:

1. **Read the plan first.** Find the owning phase in the master migration plan and
   the supporting design/frontend/backend/testing plan before writing code. Build
   in phase order; do not start a slice before its dependency exists.
2. **Keep entity/route/field names identical** between code and the owning plan.
   If the code needs a name the plan doesn't have (or vice versa), change the plan
   in the same commit — don't let them diverge.
3. **Mark phase progress.** When a phase's acceptance criteria are all verified,
   record the outcome in the master plan and `wiki/log.md`.
4. **Update `wiki/__plans/README.md`** whenever a plan file is added, renamed, or
   removed so the index stays complete.
5. **Run the quality gate before finishing:** `scripts/quality-gate.sh` (which runs
   `scripts/check-plan-drift.sh`, backend tests, and the frontend build/lint/test).
   The drift check must report **0 fail**; warnings flag forward-looking
   convergence and are acceptable until the owning phase lands.

## Non-negotiable Invariants (enforced by the drift check + CI)

- **API-first.** The React frontend calls the HomeScout API; it never owns agent
  orchestration and never imports an agent/LLM SDK directly. Agent work sits behind
  the API service boundary.
- **Microsoft Foundry Agent Service** is the target agent platform; do not use
  classic Foundry agents for new work. Direct model calls use the OpenAI SDK against
  Foundry `/openai/v1`; project/agent/index/evaluation/tracing work uses the
  Foundry SDK.
- **Not mortgage advice.** No regulated mortgage-product recommendation; no
  simplistic safe/unsafe area label; cost estimates always ship with their
  assumptions. Prefer explainable comparisons, evidence trails, and clear caveats.
- **Expected failures use FluentResults**, not exceptions, in the service layer.
  Do not introduce `ErrorOr` or other Result libraries.
- **Every slice ships tests** for the surfaces it changes (backend
  unit/contract/integration; frontend unit/component/E2E).
- **Plans stay indexed and linked.** Every plan file is listed in
  `wiki/__plans/README.md` and its relative links resolve.

The drift rules and scan commands live in
[`wiki/__plans/README.md`](wiki/__plans/README.md) under "Detecting Plan Drift".
CI runs `scripts/check-plan-drift.sh` on every PR (`.github/workflows/plan-drift.yml`).

## Git Workflow

- Work each phase on its own branch (`migration/phase-N-*` for the migration; a
  short descriptive branch otherwise). Never commit directly to `main`.
- Open a PR per phase with the acceptance criteria as a checklist; the required CI
  checks (`plan-drift`, `backend-ci`, `frontend-ci`) must be green to merge.
- Branch the next phase from the merged `main`.

## Attribution Rules

- Agents must not add themselves as a co-author or collaborator when commenting,
  committing, or opening pull requests.
- Do not append `Co-Authored-By` trailers, agent signatures, or "generated by"
  attribution lines to commits, PRs, issues, or code comments.
- Keep authorship attributed to the human developer only.

## Frontend Design Rules

- Follow `wiki/frontend-design-guidelines.md` strictly for all frontend work.
- Do not ship frontend changes that contradict the documented design direction
  unless the design page is updated in the same change with a clear rationale.
- Build product UI, not tutorial UI. Starter-template pages are scaffolding until
  replaced.

## Course Alignment

- Keep the course-to-product mapping current in
  `wiki/__plans/00-roadmap/course/course-playlist-tracker.md`.
- Before course-aligned implementation work, use
  `wiki/__plans/00-roadmap/release-monitoring.md` to check for new playlist videos
  and companion repo commits.
- When implementation differs from a plan or the course, update the plan file and
  record the divergence in `wiki/__plans/00-roadmap/plan-divergence.md`.

## Wiki Maintenance

1. After any significant implementation work, update relevant wiki pages.
2. After ingesting new requirements or specs, create or update wiki pages.
3. Keep `wiki/index.md` current. List all pages with one-line summaries.
4. Keep `wiki/log.md` current. Append an entry for each session's work.
5. Never modify `wiki/raw/`. It is for immutable source documents only.
6. Use kebab-case page filenames, for example `state-management.md`.
7. Cross-reference between pages using `[[Page Name]]` style links; use relative
   Markdown links only when pointing at exact files or external URLs.
8. If new work contradicts an existing wiki page, update it and note what changed
   in `wiki/log.md`.

## Copilot Instructions

`.github/copilot-instructions.md` intentionally points here. Treat `AGENTS.md` as
the canonical instructions source for Codex, Copilot, Claude, and other coding
agents in this repository.
