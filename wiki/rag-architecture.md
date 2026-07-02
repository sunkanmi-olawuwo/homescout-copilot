# RAG Architecture

HomeScout should not use the usual "index a company document corpus and chat with it" pattern as the main product shape. The better fit is case-file RAG plus a curated HomeScout knowledge base, orchestrated by backend agents and tools.

## User-Owned Case File

The user-owned case file is the main private corpus for a comparison session or saved property search.

It can include:

- property listings
- EPC PDFs
- survey excerpts
- floorplans
- viewing notes
- estate-agent messages
- photos and screenshots
- buyer preferences

This corpus changes per user, property, and comparison. It should be private, user-scoped, and deleted or retained according to clear product rules.

## Curated HomeScout Knowledge Base

The curated knowledge base is the stable product knowledge layer.

It can include:

- UK homebuying process explainers
- EPC terminology
- survey terminology
- cost-estimation assumptions
- crime, amenity, and school data interpretation notes
- safety rules and "not mortgage advice" boundaries
- source reliability guidance

This layer should be versioned with the product and treated as part of prompt, evaluation, and safety governance.

## Curated Source Strategy

The curated knowledge base should not be a raw scrape of external websites. It should contain short HomeScout-authored notes, checklists, definitions, assumptions, and safety rules derived from authoritative sources.

Each entry should record:

- source title
- source URL
- publisher
- licence or usage note
- date reviewed
- HomeScout summary
- product rule or retrieval purpose

Good seed sources:

- GOV.UK home buying and selling guidance for process explanations and buyer/seller paperwork.
- GOV.UK energy certificate services for EPC meaning, access, and limitations.
- HM Land Registry Price Paid Data for price-paid context, attribution, permitted use, and data caveats.
- RICS consumer guidance for home survey terminology and survey levels.
- GOV.UK flood-risk services for flood-risk explanation and source linking.
- Police.uk data/API documentation for crime-context interpretation and caveats.
- OpenStreetMap and Overpass documentation for amenity retrieval and usage limits.
- Get Information about Schools or equivalent official education datasets for school-source notes.
- Internal HomeScout rules for "not mortgage advice", no definitive valuation, no simplistic crime safety scoring, and missing-information handling.

Use external sources in two different ways:

- Curated knowledge entries: stable explanatory notes that HomeScout owns and can evaluate.
- Live tools: data calls that should stay fresh, such as price-paid records, amenities, crime context, schools, and flood-risk lookups.

Do not index copyrighted third-party articles wholesale. Link to them as references only when useful, and write HomeScout's own concise interpretation.

## Retrieval Shape

The backend agent/retrieval planner should decide which source type is needed:

- user case-file retrieval for property-specific evidence
- curated knowledge-base retrieval for terminology, interpretation, and safety boundaries
- deterministic tools for calculations
- live data tools for external public data
- missing-information responses when evidence is not available

The frontend should show source categories clearly so users can distinguish uploaded evidence, product guidance, live data, and estimates.

## Phase Fit

- Phase 2 starts the curated knowledge base as source-controlled assumptions and safety rules.
- Phase 3 links prompts and evaluations to curated knowledge-base entries.
- Phase 5 introduces saved comparisons and user scoping needed for private case files.
- Phase 6 adds uploads, extraction, indexing, and retrieval over case-file documents.
- Phase 7 adds deployment, monitoring, tracing, and cost management for retrieval and indexing.
- Phase 8 adds buyer preferences and spoken notes into the case file with inspectable memory rules.

See [[Phased Learning And Build Plan]], [[API-First Foundry Agents]], and [[Testing Strategy]].
