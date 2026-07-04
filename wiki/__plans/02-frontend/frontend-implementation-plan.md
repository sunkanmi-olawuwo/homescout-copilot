# Frontend Implementation Plan

*Review the design, then build.*

This is the **frontend build phase**: take the finished design and implement the real
HomeScout product UI against it. It has two stages — **(1) review the design** (the
[Design Brief](./design-brief.md) plus whatever the design agent produces), then
**(2) implement** the React app screen-by-screen against the design system, wired to the
HomeScout API.

It follows the binding design direction in `wiki/frontend-design-guidelines.md` and the
API-first invariant: the React app calls `HomeScoutCopilot.API`; it never owns agent
orchestration and never imports an agent/LLM SDK.

> **Codex:** start from [Codex Frontend Instructions](./codex-frontend-instructions.md) — the
> concrete handoff for building from the finished Claude Design.

## Inputs (must exist before Stage 2)

- ✅ **Finished design** — the interactive prototype `wiki/raw/HomeScout Copilot.html` (source
  of truth in Claude Design). Open it in a browser; lift tokens/copy per the Codex
  instructions. This is the primary design input; the brief below is the written spec behind it.
- ✅ [Design Brief](./design-brief.md) — full-scope spec (vision, IA, screens, copilot UX,
  design tokens, states, accessibility, deliverables).
- ✅ Backend contracts already live: `GET /api/status`, `GET /api/comparison/sample`,
  `POST /api/mortgage/estimate`, `GET /api/mortgage/base-rate`, `POST /api/copilot/ask`
  (see [[Endpoint Summary]]); the typed `HomeScoutApiClient` exists.

## Stage 1 — Review the design (gate before building)

- Read the design-agent output against the Design Brief and
  `wiki/frontend-design-guidelines.md`; reconcile any conflict by updating the brief (or
  the guidelines) in the same change with a rationale — never silently diverge.
- Extract the **design tokens** into a single source of truth (CSS custom properties /
  theme file) matching brief §11.
- Confirm the **information architecture** (§5) and the **component inventory** (§12) are
  complete and map to real API data; flag any screen that needs an endpoint we don't have
  yet and route it to the owning backend phase.
- Record the reviewed decisions in [[Component Architecture]] and, where the design
  changes product behaviour, in [[Plan Divergence]].

**Stage 1 exit:** tokens captured in code; component inventory + screen list agreed;
data-to-UI mapping confirmed; no unresolved design/guideline conflicts.

## Stage 2 — Implement (design-system first, then screens)

Build outside-in from the design system so screens compose from tested primitives:

1. **Design system in code** — tokens (§11) as CSS variables + a small theme; base
   primitives (buttons, inputs, cards, badges, tabs) with **every state** (§13: default,
   hover, focus, active, disabled, loading, empty, error).
2. **Data & trust primitives** (§8) — the reusable facts / estimates / assumptions /
   missing-data and provenance/caveat components; these encode the "not mortgage advice"
   and "no safe/unsafe label" product boundary in the UI.
3. **Screens & regions** (§6) — comparison workspace, property input, comparison/report,
   evidence panel, saved-comparisons — composed from the primitives, using realistic
   HomeScout language (no tutorial/landing scaffolding).
4. **The copilot conversation surface** (§7) — wire to `POST /api/copilot/ask`; render the
   answer with its tool calls, evidence, and caveats; the frontend is a **conversation
   surface**, not a form, and stays behind the API boundary. Degrade gracefully when the
   copilot is unconfigured (API returns 503).
5. **Key flows** (§9) end-to-end against the API via `HomeScoutApiClient`.

## Testing & quality (every slice ships tests)

- **Vitest + Testing Library** component tests for primitives and each screen region,
  covering the design's state matrix (§13).
- **Playwright E2E** smoke for the design-critical flows (§9) — extends the Phase-4 e2e
  already in `frontend-ci.yml`.
- **Accessibility**: WCAG 2.2 AA per §15 — keyboard paths, visible focus, contrast, roles;
  add automated a11y assertions where practical.
- No agent/LLM SDK import in the frontend (drift check enforces API-first).

## Acceptance criteria

- Design tokens exist as a single in-code source of truth matching the brief/design-agent
  spec; primitives implement all states in §13.
- The reviewed screens (§6) and the copilot surface (§7) are implemented against real API
  data via `HomeScoutApiClient`, in HomeScout product language (no starter UI remaining).
- Component + E2E tests cover the primitives and the key flows; a11y checks pass.
- `pnpm run build && pnpm run lint && pnpm run test && pnpm run e2e` green; quality gate
  green; drift 0 fail.

## Verification

- `cd frontend && pnpm run build && pnpm run lint && pnpm run test && pnpm run e2e`
- `bash scripts/quality-gate.sh` → all green
- Manual review of each implemented screen against the design-agent deliverable + brief.

## Sequencing

Runs as the **frontend track** alongside the backend phases in the
[Phased Learning And Build Plan](../00-roadmap/phased-learning-build-plan.md): the early
workspace **shell** landed in that plan's Phase 1; this plan is the **full design
implementation** that begins once the design-agent deliverables exist, and each screen ships
as its own branch → PR → gated merge. Screens depending on not-yet-built endpoints wait for
their owning backend phase.
