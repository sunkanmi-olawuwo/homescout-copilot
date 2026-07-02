# Frontend Design Guidelines

These rules are binding for HomeScout frontend work.

## Product Feel

HomeScout should feel like a serious decision-support workspace for homebuyers. The interface should be calm, structured, and useful under repeated use.

Prefer:

- comparison tables
- saved-search sidebars
- evidence panels
- source badges
- assumption callouts
- compact forms
- map/report split views
- clear empty states

Avoid:

- marketing-style landing pages
- oversized hero sections
- decorative gradients as the main visual idea
- generic tutorial UI
- card-heavy pages with no workflow
- vague AI magic language

## First Screen

The first screen should be the usable product workspace:

- prompt/composer area
- saved comparisons or search history
- current comparison/report area
- preference summary or evidence panel when relevant

Do not make the first screen a pure landing page unless the project explicitly adds a separate public marketing site.

## Layout

- Use stable responsive dimensions for sidebars, comparison panels, input controls, and result cards.
- Ensure text never overflows its container on mobile or desktop.
- Keep cards for repeated entities, modal content, or genuinely framed tools.
- Do not nest cards inside cards.
- Prefer full-width application bands or unframed workspace regions over floating decorative sections.

## Visual Style

- Use a restrained, professional palette.
- Avoid one-note palettes dominated by a single hue.
- Avoid decorative orbs, bokeh blobs, or unrelated background art.
- Use icons for common actions where available.
- Text inside compact UI must use compact type, not hero-scale headings.

## Interaction

- Use tooltips for unfamiliar icons.
- Use toggles for binary settings.
- Use sliders or numeric inputs for values like budget, deposit, rate, term, or radius.
- Use tabs for major workspace views.
- Use menus for option sets.
- Use clear buttons for commands.

## Domain UI Expectations

HomeScout users should be able to:

- compare multiple properties or locations
- inspect assumptions behind estimates
- see data source and freshness where available
- save searches
- review preferences
- upload listing/survey/EPC/floorplan files
- ask follow-up questions without losing context

## Review Requirement

Before frontend work is considered complete:

- verify desktop and mobile layouts
- check text fit
- check color balance
- confirm the screen advances a HomeScout workflow
- update this page if the design direction intentionally changes

