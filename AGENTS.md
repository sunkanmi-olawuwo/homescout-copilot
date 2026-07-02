# Agent Instructions

This repository uses `wiki/` as the canonical development memory for HomeScout Copilot. `CLAUDE.md` must remain a symlink to this file so Claude, Codex, and other assistants follow the same project rules.

## Required Documentation Rules

1. After any significant implementation work, update relevant wiki pages.
2. After ingesting new requirements or specs, create or update wiki pages.
3. Keep `wiki/index.md` current. List all pages with one-line summaries.
4. Keep `wiki/log.md` current. Append an entry for each session's work.
5. Never modify `wiki/raw/`. It is for immutable source documents only.
6. Page naming must use kebab-case filenames, for example `state-management.md`.
7. Cross-reference between pages using `[[Page Name]]` style links.
8. If new work contradicts an existing wiki page, update it and note what changed in `wiki/log.md`.

## Product Rules

- HomeScout Copilot is a homebuying due-diligence assistant, not a regulated mortgage adviser.
- Avoid language that recommends a specific mortgage product or presents estimates as advice.
- Prefer explainable comparisons, evidence trails, and clear caveats.
- Keep the course-to-product mapping current in `wiki/plan/course-playlist-tracker.md`.

## Frontend Design Rules

- Follow `wiki/frontend-design-guidelines.md` strictly for all frontend work.
- Do not ship frontend changes that contradict the documented design direction unless the design page is updated in the same change with a clear rationale.
- Build product UI, not tutorial UI. Starter-template pages should be treated as scaffolding until replaced.

## Plan Divergence Workflow

- Canonical plan files live under `wiki/plan/`.
- Legacy `docs/` entries should point to the canonical wiki files, preferably by symlink.
- When implementation differs from a plan, update the plan file and record the divergence in `wiki/plan/plan-divergence.md`.

