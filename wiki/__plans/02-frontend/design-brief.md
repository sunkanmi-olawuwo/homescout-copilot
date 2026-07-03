# HomeScout Copilot — Design Brief (for the design agent)

This is the complete design specification for HomeScout Copilot. It is written to be
handed to a design agent to produce a **premium, world-class** product design covering
the **full scope** of the project. The binding rules in
[[Frontend Design Guidelines]] still apply; this brief expands them into vision,
personas, information architecture, every screen and flow, the copilot conversation UX,
a design-system token spec, states, trust/safety UI, accessibility, and deliverables.

> Non-negotiables carried from the product: HomeScout is a **homebuying due-diligence
> copilot, not a regulated mortgage adviser**. Every estimate ships with its
> assumptions and a "not mortgage advice" caveat. **Never** label an area simply safe
> or unsafe. Prefer explainable comparisons, evidence trails, and clear caveats.

---

## 1. Product vision & positioning

HomeScout is a **calm, trustworthy workspace** where a UK homebuyer talks to a copilot
that reasons over public data, uploaded documents, and their own preferences to compare
properties and neighbourhoods — surfacing lifestyle fit, affordability trade-offs, and
the questions worth asking *before* they book viewings or pay professionals.

Position it at the intersection of three credible references (the *feeling*, not the
skin): the **clarity of modern fintech** (Monzo/Nubank — friendly precision), the
**information density and keyboard-first calm of a professional tool** (Linear/Bloomberg
terminal — but humane), and the **quiet authority of civic/government data services**
(GOV.UK — sober, sourced, honest about uncertainty). The result should feel *premium*
through restraint, typographic craft, and trustworthy data presentation — not through
decoration.

## 2. Brand personality & voice

- **Trustworthy, precise, calm, on the buyer's side.** Never hypey, never "AI magic".
- Explains its reasoning and shows its sources. Comfortable saying *"we don't know"*.
- UK English. Careful, non-alarming language around money, crime, schools, and safety.
- Confident but humble: it informs and frames trade-offs; it does not decide or advise.

## 3. Users & jobs-to-be-done

Primary persona — **Amara, 29, first-time buyer, London/commuter belt.** Overwhelmed by
listings and jargon (EPC, leasehold, service charge, stamp duty). Wants to compare a few
areas/properties fast, understand true monthly cost, and know what to ask at a viewing.

Secondary — **Tom & Priya, second-steppers with kids.** Care about schools, commute,
space, and long-term value; compare shortlisted properties in detail; upload surveys.

Jobs-to-be-done:
- "Compare these 2–3 areas/properties across the things I care about."
- "What would this actually cost me per month?"
- "Is this listing/EPC/survey telling me something I should worry about?"
- "What should I ask at the viewing?"
- "Remember my budget and priorities so I don't repeat myself."

## 4. Design principles

1. **Workspace, not chatbot.** The copilot lives *inside* a working comparison
   workspace; conversation drives structured, inspectable output.
2. **Evidence over assertion.** Every figure links to its source, freshness, and
   assumptions. Separate **facts / estimates / assumptions / missing data** visually.
3. **Explainable and honest.** Show the tools the copilot used, and what it couldn't
   find. Uncertainty is a first-class state, not an error.
4. **Safety by design.** The "not mortgage advice" caveat and assumption callouts are
   built into components, not bolted on. No safe/unsafe area verdicts.
5. **Dense but calm.** High information density achieved through hierarchy, whitespace,
   and typography — never clutter.
6. **Fast and keyboard-friendly.** Power-user affordances (command palette, shortcuts)
   without punishing newcomers.
7. **Accessible to everyone** (WCAG 2.2 AA minimum).

## 5. Information architecture (full app map)

```
HomeScout Copilot
├── Comparison Workspace  (the home / default screen)
│   ├── Saved-searches sidebar (history, new comparison, search)
│   ├── Conversation panel (the copilot: messages, streaming, tool calls, follow-ups)
│   ├── Comparison canvas (the structured result: area/property columns)
│   ├── Evidence panel (facts/estimates/assumptions/missing; sources + freshness)
│   └── Composer (multimodal input: text, attach, voice)
├── Tool / capability surfaces (invoked in-conversation and inspectable)
│   ├── Mortgage cost estimator (inputs + breakdown + +3% stress + caveat)
│   ├── Base-rate context (labelled context, provenance)
│   ├── Crime context (Police.uk) — contextual, never a safety score
│   ├── Amenities (OpenStreetMap) — grouped by type, with distance
│   ├── Schools (GOV.UK data) — nearby + key public metrics
│   ├── Price context (HM Land Registry) — sold-price context + caveats
│   └── Commute (TfL) — journey/time context for London
├── Case file (per comparison): uploads — listings, EPC, survey, floorplans, photos,
│   notes; extracted facts; document viewer
├── Preferences / memory: budget, deposit, commute tolerance, schools, lifestyle —
│   inspectable and editable; "what HomeScout remembered and why"
├── Saved comparisons: list, rename, reopen, delete
├── Account / auth (later phase): sign in, private workspace
├── Onboarding / first run: empty states, sample comparison, guided first prompt
└── Settings: assumptions defaults, units, theme, data & privacy, disclaimers
```

The **default screen is the working Comparison Workspace**, never a marketing landing
page.

## 6. Screens & regions (what to design)

Design each of the following at **desktop and mobile**, with all relevant states
(§13). "Region" = part of the workspace; "screen" = a distinct view/route.

1. **Comparison Workspace (default).** Three-region layout: saved-searches sidebar ·
   central column (conversation + comparison canvas) · evidence panel. Collapsible
   sidebar and evidence panel. The composer is pinned.
2. **Conversation panel.** The copilot dialogue (see §7 in depth): user + assistant
   messages, streaming, tool-call chips, inline evidence citations, suggested
   follow-ups, the persistent not-advice caveat, and the *not configured* / error
   states.
3. **Comparison canvas.** The structured, scannable comparison: 2–3 **area/property
   columns** compared across rows (monthly cost, commute, crime context, schools,
   amenities, price context, EPC, tenure). Each cell shows value + source badge +
   freshness; estimates and assumptions are visually distinct. Row expand for detail.
4. **Evidence panel.** For the focused item/answer: **Facts**, **Estimates**,
   **Assumptions**, **Missing data** sections; each entry with source badge, freshness,
   and provenance (`Live` / `Cache` / `Fallback`). Links back to the message that used
   it.
5. **Mortgage cost estimator.** A focused tool view/panel: inputs (property price,
   deposit ± slider, interest rate — *the buyer's own figure*, term, repayment vs
   interest-only); output breakdown (monthly payment, total interest, total repayment,
   LTV, **+3% stress payment**); the assumptions list and the *not mortgage advice*
   caveat. Base-rate shown only as labelled *context*.
6. **Tool detail views** (crime, amenities, schools, price, commute) — inspectable
   panels/cards with the source, freshness, a small map or list/mini-chart, and honest
   caveats (e.g., crime is *contextual*, never a safety verdict).
7. **Case file / uploads.** Drag-drop upload of listing/EPC/survey/floorplan/photos;
   file list with type + status; a document viewer with extracted-facts side panel.
8. **Preferences / memory.** Editable buyer profile (budget, deposit, commute
   tolerance, school needs, lifestyle priorities); a clear "what was remembered and why
   it affected an answer" view; per-item forget/edit.
9. **Saved comparisons list.** Grid/list of saved sessions (title, areas, last edited);
   rename, reopen, delete.
10. **Onboarding / first run.** Warm-but-sober empty state: one-line what-it-does, a
    prefilled example prompt, and a sample comparison the user can open. No hero page.
11. **Account / auth** (later): minimal sign-in; private workspace framing.
12. **Settings.** Assumption defaults (insurance %, maintenance %), units, **theme
    (light/dark)**, data & privacy, and the disclaimers.
13. **Global chrome.** App bar / brand, command palette (⌘K), notifications/activity,
    account menu, and a persistent, unobtrusive "decision support, not advice" note.

## 7. The copilot conversation — in depth

This is the heart of the product; design it richly.

- **Message types:** user (text / attachment / voice-transcribed); assistant
  (streamed prose); **tool-call chips** (e.g. `estimate_mortgage`, `get_base_rate`,
  `crime`, `schools`) shown as the agent works, each expandable to its inputs/result
  and linking into the evidence panel; **structured blocks** the assistant emits
  (a cost breakdown card, a comparison snippet, a source list).
- **Streaming:** token-by-token assistant text with a calm typing indicator; tool
  chips appear/settle as calls complete; never a frozen spinner.
- **Evidence trail:** inline citations/superscripts link claims to sources; a message
  footer summarises the tools used and assumptions made; one click opens the evidence
  panel focused on that answer.
- **Grounding & honesty:** figures always attributed; if the copilot lacks data it
  says so and offers what it *can* do. Assumptions are surfaced, not hidden.
- **Guidance:** suggested prompt chips ("Compare monthly cost", "What should I ask at
  the viewing?", "Add a third area") — especially in the empty state.
- **Safety:** the *not mortgage advice* line is a persistent, quiet part of the
  conversation frame; the copilot never recommends a product or gives a safe/unsafe
  verdict.
- **System states in-conversation:** *thinking/streaming*, *tool running*, *degraded*
  (a data source fell back — shown honestly), *not configured* (copilot unavailable →
  a calm explanatory state, mirroring the API's 503), and *error/retry*.

## 8. Data & trust UI (design these as reusable primitives)

- **Source badge** — small, quiet chip naming the source (Police.uk, HM Land Registry,
  GOV.UK, OpenStreetMap, TfL, Bank of England, "Your document") + a freshness
  timestamp; hover/press for detail.
- **Provenance tag** — `Live` / `Cache` / `Fallback` for values fetched from external
  sources (mirrors the API), so users know how current a figure is.
- **Assumption callout** — an inline, expandable "based on these assumptions…" list
  attached to any estimate.
- **Missing-data state** — a distinct, non-alarming "we couldn't find this / it varies"
  treatment; never a red error for absent public data.
- **Caveat frame** — the standard "This is an estimate, not mortgage advice — speak to
  a qualified adviser" line, styled as calm guidance.
- **Fact vs estimate vs assumption** — three visually distinct value styles so users
  never mistake a modelled figure for a hard fact.

## 9. Key flows to prototype

1. **Compare two areas.** Empty workspace → type/prefill a prompt → streamed answer +
   comparison canvas fills → open evidence → add a third area.
2. **Estimate monthly cost.** Ask a cost question → estimator runs → breakdown card in
   conversation + full estimator panel → adjust rate/term → see stress figure.
3. **Upload a listing/EPC/survey.** Attach in composer → file appears in case file →
   extracted facts feed the comparison and a follow-up answer.
4. **Save & resume.** Name a comparison → reopen later from the sidebar with full
   context.
5. **Edit a preference.** Change budget/commute tolerance → see how it reframes a
   subsequent answer; inspect "what was remembered".
6. **Degraded/blocked source.** A tool falls back or a source is unavailable → honest
   provenance/missing-data treatment, not a hard failure.

## 10. Data visualization

- **Comparison table/matrix** as the spine of the canvas — scannable, sortable,
  responsive (columns → stacked cards on mobile).
- **Cost breakdown** — clear numeric hierarchy; optional simple bar for
  principal-vs-interest and the +3% stress delta.
- **Maps** — light, unobtrusive area/amenity maps (pins grouped by type, distance
  rings). Never the dominant decorative element.
- **Mini-charts** — sparklines/small bars for price context or trends, always with
  caveats about freshness/limits.
Keep all data-viz restrained, labelled, and honest about uncertainty.

## 11. Design system spec (tokens the design must define)

- **Color.** A restrained, professional core palette (a trustworthy primary + neutral
  greys), **not** dominated by a single hue. Define **semantic** colors (success/
  info/warning/critical used sparingly and never for absent public data), a **data-viz**
  categorical palette (colour-blind safe), and **status/provenance** colors
  (Live/Cache/Fallback; fact/estimate/assumption/missing). Full **light and dark**
  themes with verified contrast.
- **Typography.** A precise, legible type system (a humanist sans for UI; consider a
  mono/tabular face for figures so numbers align). Define a compact type scale — compact
  UI uses compact type, not hero headings. Tabular numerals for all money/metrics.
- **Spacing & grid.** An 4/8px spacing scale; a responsive workspace grid; stable
  dimensions for sidebar, panels, composer, and cards.
- **Radius, elevation, borders.** Subtle, consistent; prefer hairline separators and
  restrained elevation over heavy shadows. No card-in-card.
- **Iconography.** A single consistent icon set; icons for common actions; tooltips for
  unfamiliar ones.
- **Motion.** Purposeful, quick, calm (streaming, panel collapse, tool-chip settle);
  respect `prefers-reduced-motion`.

## 12. Component inventory (define + all states)

App shell / three-region layout; collapsible sidebar; saved-search list item;
composer (text + attach + voice + send) with disabled/streaming states; message bubbles
(user/assistant); tool-call chip (running/done/error, expandable); structured
cost-breakdown card; comparison table + cell (fact/estimate/assumption/missing);
column header (area/property); evidence panel + evidence item; source badge; provenance
tag; assumption callout; caveat banner; slider + numeric input (budget/deposit/rate/
term/radius); toggle; tabs; menu; command palette; file-drop + file row + document
viewer; preference item (view/edit/forget); empty states; skeleton/loading; toast/
inline error; the *not configured* (503) state; modal/drawer; tooltip.

## 13. States & feedback (for every surface)

Design: **default**, **loading/skeleton**, **streaming**, **empty/first-run**,
**partial** (some tools returned, some pending), **degraded** (a source fell back),
**missing data**, **error/retry**, **offline**, **permission/auth-required**, and
**not-configured (503)**. None of these should feel like a dead end; each offers a next
step.

## 14. Responsive & breakpoints

Desktop-first workspace (≥1280 three regions). **Tablet** (≥768): sidebar and evidence
collapse into drawers; conversation + canvas remain primary. **Mobile** (<768): single
column with a bottom composer; comparison columns become stacked, swipeable cards;
sidebar/evidence as sheets. Text must never overflow; touch targets ≥44px.

## 15. Accessibility

WCAG 2.2 AA minimum. Full keyboard operation (composer, tool chips, canvas, panels,
command palette); visible focus; logical tab order; ARIA landmarks/roles for the three
regions and live-region announcements for streaming and tool results; contrast verified
in light and dark; `prefers-reduced-motion` and `prefers-color-scheme` honored; do not
convey meaning by colour alone (fact/estimate/assumption also differ by label/shape).

## 16. Content & microcopy

UK English. Money/area/crime/school language is careful and non-alarming. Standard
caveat: *"This is an estimate, not mortgage advice — speak to a qualified mortgage
adviser."* Crime is always *context*, never a score or verdict. Empty and error copy is
warm, specific, and offers a next action. Prefer "estimate", "context", "comparison",
"questions to ask", "assumptions".

## 17. Constraints & platform

React + Vite front end that calls the HomeScout API (`POST /api/copilot/ask`,
`/api/mortgage/estimate`, `/api/mortgage/base-rate`, `/api/comparison/*`); it never owns
agent orchestration. Design must map to buildable components and to the API's real
states (streaming answer, tool calls, provenance, 503 when the copilot isn't
configured). Respect the anti-patterns in [[Frontend Design Guidelines]].

## 18. Anti-patterns (do not do)

Marketing landing pages; oversized hero sections; decorative gradients/orbs/bokeh as the
main idea; generic tutorial UI; card-heavy pages with no workflow; card-in-card;
one-note single-hue palettes; "AI magic" language; presenting estimates as facts or as
advice; any safe/unsafe area verdict; red errors for merely-absent public data.

## 19. Deliverables (what the design agent should produce)

1. A short **visual language / direction** (mood, principles applied).
2. **Design tokens**: full color (light + dark), type scale, spacing, radius,
   elevation, motion.
3. **Every screen/region in §6**, at desktop and mobile, in the key states in §13.
4. The **copilot conversation** designed richly (§7): streaming, tool chips, evidence
   citations, structured blocks, empty/degraded/not-configured states.
5. The **trust/data primitives** (§8) and **component inventory** (§12) as a component
   sheet with states.
6. **Prototype flows** for §9 (at least: compare two areas; estimate monthly cost;
   upload a document).
7. **Data-viz** patterns (§10): comparison table, cost breakdown, map, mini-chart.
8. An **accessibility annotation** pass (§15).

## 20. References (the feeling, not the skin)

Modern fintech clarity (Monzo/Nubank), professional-tool calm and density
(Linear, a humane Bloomberg), and civic-data trustworthiness (GOV.UK). Premium via
typographic craft, restraint, and honest data presentation — never decoration.
