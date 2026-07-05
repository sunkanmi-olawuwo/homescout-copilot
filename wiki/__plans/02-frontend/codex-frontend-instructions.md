# Codex Frontend Instructions — Build From The Claude Design

*Handoff for Codex (the frontend agent). Read [[Work Tracks]] and
[Frontend Implementation Plan](./frontend-implementation-plan.md) first.*

You are building the **HomeScout Copilot** React frontend to match a finished Claude Design.
This page tells you how to work with the design source and exactly what to build first.

## The design source

- **Live reference (in this repo):** `wiki/raw/HomeScout Copilot.html` — a self-contained,
  interactive prototype of the whole workspace. **Open it in a browser to see the real
  design** (layout, theming, the copilot flow, responsive behaviour). It renders live.
- **Editable source of truth:** the `HomeScout Copilot.dc.html` Design Component lives in
  **Claude Design** (project `ea777567-868a-4ba5-ad10-a5af5cec58bb`), edited there with the
  `dc_*` canvas tools. You do **not** edit it from this repo.
- **Never edit** `wiki/raw/HomeScout Copilot.html` — it is a compiled snapshot (immutable
  source, per the wiki rules). If the design changes, it's re-exported from Claude Design.

The prototype is **Vue + a scripted phase machine** (a demo reveal). You are **not** porting
Vue — you are re-implementing the *visual + interaction design* in **React + Vite** at
`frontend/`, wired to the **real HomeScout API** instead of the scripted demo data.

## How to work with it

1. **Open the HTML in a browser** to experience the design; resize to see the three responsive
   layouts.
2. **Lift the design tokens** verbatim from the `<helmet>`'s `:root`, `[data-theme="light"]`
   and `[data-theme="dark"]` blocks into your React theme as CSS custom properties. The theme
   is **scoped on the app's own root element** — do the same so the host page can't override it.
3. **Read the copy + figures** from the template markup and the `renderVals()` data arrays
   (comparison rows, evidence groups, tool list, follow-ups, the mortgage math). Use these as
   the content model; replace the scripted values with real API data.

## Design system to reproduce

- **Fonts:** `IBM Plex Sans` (UI), `IBM Plex Mono` (figures / numeric / mono). Self-host or
  load the same way the design does.
- **Theming:** dark + light, via `var(--token)` on a scoped root. Key token groups (exact hex
  in the helmet):
  - Surfaces: `--bg`, `--raise`, `--sunken`, `--border`, `--border-strong`, `--hair`.
  - Brand: `--navy` (header), `--navy-fg`, `--accent`, `--accent-2`, `--accent-soft`, `--accent-line`.
  - Semantics: `--pos` / `--pos-soft`, `--crit` / `--crit-soft`, `--caveat*` (the
    not-mortgage-advice callout).
  - **Provenance badges:** `--live`, `--cache`, `--fallback` — use these for the
    Live/Cache/Fallback provenance chips (they match the backend's provenance model).
  - Data-viz palette: `--dv1`…`--dv5` (comparison canvas).
  - Elevation: `--shadow`, `--shadow-lg`.
- **Spacing/radius** are inline literals in the design (not tokenised) — define a small,
  consistent scale in React and apply it.

## Layout & responsive

Three regions (see the prototype):

- **Header** (~52px, navy) — brand + global actions.
- **Left sidebar** — Saved comparisons + nav: *Area comparison*, *Mortgage cost estimator*,
  *Settings*.
- **Main** — the copilot conversation + comparison/report (center tabs).
- **Right aside** — the **Evidence panel** (extracted facts; a drop target for
  *listing / EPC / survey / floorplan*).

Breakpoints (mirror `state.vw`): **mobile `< 760`** (stacked cards), **tablet `< 1180`**
(side panels become overlays), **desktop `≥ 1180`** (three regions). Read the viewport in an
effect on mount + on resize.

## Wire to the API (the seam — do not mock where a real endpoint exists)

Use the typed `HomeScoutApiClient` / documented endpoints ([[Endpoint Summary]]). **Never
import an agent/LLM SDK** — API-first is enforced by the drift check.

| Design surface | Endpoint | Notes |
| --- | --- | --- |
| Mortgage cost estimator | `POST /api/mortgage/estimate`, `GET /api/mortgage/base-rate` | **Live + deterministic — build this first, end to end.** |
| Copilot conversation | `POST /api/copilot/ask` | Returns **503 until Foundry is provisioned** — degrade gracefully (show composer; queued/unavailable state). |
| Area comparison | `GET /api/comparison/sample` | Sample data only for now; the rich Greenwich/Croydon comparison is **future backend work** — render sample + clearly-marked placeholders. |
| Status | `GET /api/status` | Health/first call. |

If you need a field or endpoint the API doesn't expose (e.g. structured evidence items),
**request it from the backend track** — do not invent an endpoint or reach past the API
(seam-first; contract changes are backend-led).

## Domain guardrails (non-negotiable — from the design + product boundary)

- Keep the **"not mortgage advice"** caveat on every cost view.
- **Crime is context, never a verdict** — no simplistic safe/unsafe area label.
- **Every figure is tagged** `fact` / `estimate` / `assumption` / `missing`, with a **source**
  and a **provenance** badge (`live` / `cached` / `fallback`).

## Process

- React + Vite at `frontend/`; **design-system first** (tokens → primitives → screens).
- Tests every slice: Vitest component tests (cover the state matrix), Playwright E2E for key
  flows, WCAG 2.2 AA.
- Own `frontend/` only; don't touch `dotnet/` or `Shared` DTOs. Branch `feature/fe-*`; keep
  `frontend-ci` green.

## First slice (iteration 1)

1. **Foundation:** design tokens (both themes, scoped root) + IBM Plex fonts + the app shell
   (header + left sidebar + main + right evidence panel), responsive across the three
   breakpoints.
2. **Mortgage cost estimator, end to end** against `POST /api/mortgage/estimate` +
   `GET /api/mortgage/base-rate`: property price, deposit, LTV, loan amount, interest rate,
   repayment / interest-only → monthly payment + stress test + running costs (service charge,
   buildings insurance, maintenance reserve, council tax) + the BoE base-rate reference.
   Figures tagged + provenance badges; the not-mortgage-advice caveat present.

This slice is **fully API-backed and needs no Foundry**, so it can land and be reviewed while
the backend brings the copilot online.

## Second slice (iteration 2) — the copilot conversation + evidence panel

The copilot is now **live** (Foundry provisioned + verified) and the **evidence contract is
merged**, so this slice is unblocked. `POST /api/copilot/ask` returns a `CopilotAnswer`:

```ts
interface CopilotAnswer {
  text: string;
  toolCalls: { name: string; summary: string }[];
  evidence: {
    label: string;
    value: string;                       // preformatted, e.g. "£2,199" / "80.1%"
    kind: 'fact' | 'estimate' | 'assumption' | 'missing';   // lowercase — render as the chip
    source: string;
    provenance: 'Live' | 'Cache' | 'Fallback' | null;        // badge; lowercase for the CSS class
  }[];
  assumptions: string[];
  caveats: string[];
}
```

1. **Conversation answers** — send the composer text (and the START WITH cards) to
   `POST /api/copilot/ask`; render `text` + tool chips from `toolCalls`. Keep the graceful
   fallback for 503 (Foundry env not set in a given environment).
2. **Evidence panel** — populate the right-rail **Evidence** tab from `answer.evidence`: the
   `kind` chip (already lowercase — matches your `.kind-chip.*` CSS) + the `provenance` badge +
   `label`/`value`/`source`. This is the design's "evidence appears here" → populated flow.

Guardrails unchanged: keep the caveats, crime = context, every figure tagged. Build against
the contract with mocked `/api/copilot/ask` responses (as the estimator slice did); it works
live wherever the `AZURE_FOUNDRY_*` env is set.

## Third slice (readability) — collapse the hero + render markdown

Two fixes, both frontend-only. The backend prompt (v2) now returns **structured markdown**, so
the seam is ready.

Status: ✅ implemented 2026-07-05 in the React frontend. The active conversation state collapses the
empty-state hero/cards, `answer.text` is rendered with a sanitized safe markdown subset, unsafe links
are not promoted, and trailing markdown caveats are shown as the caveat callout when structured
`caveats` are absent.

1. **Collapse the hero when a conversation is active.** Today the big "Compare areas and
   properties…" hero H1 + intro + START WITH cards stay pinned above the answer. Once a
   conversation starts (a `copilotQuestion`/`copilotAnswer` exists), **replace them with a
   compact header** (small label + a "New question"/reset affordance) and let the thread take
   over. Show the full hero + cards only in the empty state (before the first question).
2. **Render the answer as sanitized markdown.** `answer.text` is now Markdown, e.g.:
   ```md
   **Estimated monthly repayment: £1,500.75**
   ## Assumptions
   - 10% deposit … LTV 90% …
   ## Context
   - stress-check at 7.5% …
   ## Next steps
   - …
   This is an estimate, not mortgage advice — speak to a qualified mortgage adviser.
   ```
   Render it with a **sanitized** markdown renderer (e.g. `react-markdown` + `rehype-sanitize`,
   or a tiny safe subset — no raw HTML). Style the bold headline, `##` sub-headings, and bullet
   lists to the design; the final caveat line should read as the caveat callout. Don't re-list
   the figures — those stay in the evidence panel (the prompt already avoids repeating them).

Keep the guardrails + evidence panel unchanged. Verify with mocked responses (Vitest +
Playwright); it renders live wherever `AZURE_FOUNDRY_*` is set.
