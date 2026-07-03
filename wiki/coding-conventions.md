# Coding Conventions

## Engineering Standards

HomeScout is production-grade software built to Microsoft's standards, not tutorial
or throwaway code. Follow Microsoft's official guidance for the stack — the .NET
Framework Design Guidelines and C#/.NET coding conventions, ASP.NET Core / Aspire /
Microsoft Foundry Agent Service guidance, and the Azure Well-Architected Framework —
and prefer the current, documented, non-deprecated API surface (verify on Microsoft
Learn at implementation time). Correctness, tests, security, accessibility, and
observability are part of every change, not afterthoughts. See `AGENTS.md` >
"Engineering Standards" for the binding rule.

## General

- Prefer clear, product-specific names over tutorial names.
- Keep course-learning scaffolding isolated and remove it once a HomeScout feature replaces it.
- Favor small services with explicit responsibilities.
- Avoid introducing abstractions before a second concrete use case exists.
- Keep user-facing language careful around financial decisions, crime, schools, and safety.

## C#

- Nullable reference types stay enabled.
- Use async APIs for I/O.
- Prefer typed options for configuration.
- Keep data-provider code separate from UI components.
- Keep agent/tool code testable by wrapping external APIs behind interfaces.
- Use records for immutable DTO-style data where appropriate.

## React

- Keep components focused and product-named.
- Keep API calls behind typed client modules rather than scattering `fetch` calls through components.
- Use component names that describe product concepts, such as `ComparisonWorkspace`, `AreaSummaryPanel`, or `SavedSearchList`.
- Keep route-level components thin and move non-trivial state into hooks or services.
- Avoid keeping starter-template screens around once their lesson purpose is complete.

## Frontend Design

Frontend design must follow [[Frontend Design Guidelines]] strictly.

Important rules:

- Build a working product screen, not a marketing landing page.
- Use dense, readable, decision-support UI.
- Do not use decorative one-note gradients, oversized hero sections, or tutorial-looking placeholder pages.
- Use stable responsive dimensions for panels, toolbars, forms, and comparison cards.
- Avoid visible instructional text about how the UI works unless it is genuinely part of the user workflow.

## Documentation

- `wiki/` is canonical.
- `wiki/raw/` is immutable after creation.
- Use kebab-case filenames.
- Use `[[Page Name]]` cross-links in wiki prose.
- Update `wiki/index.md` whenever a page is added, renamed, or removed.
- Update `wiki/log.md` after significant implementation or planning sessions.

## Git

- Commit course-note updates with the HomeScout implementation they describe.
- Commit messages should describe the HomeScout behavior, not only the course step.
- Keep generated build output untracked.

## Safety

Do not implement or phrase features as regulated mortgage advice. Prefer:

- "estimate"
- "context"
- "comparison"
- "questions to ask"
- "assumptions"
- "speak to a qualified adviser"

Avoid:

- "you should take this mortgage"
- "this property is safe"
- "this is the true value"
- "this area is bad"

