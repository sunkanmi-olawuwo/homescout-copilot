# Endpoint Summary

This page tracks routes, external API integrations, and data flow.

## Current Internal Endpoints

The project currently has the first HomeScout-shaped API endpoints plus Aspire default health endpoints.

| Area | Endpoint | Status | Purpose |
| --- | --- | --- | --- |
| API health | `/health` | Scaffolded | Health check used by Aspire. |
| API liveness | `/alive` | Scaffolded | Liveness check from Aspire service defaults. |
| Product status | `/api/status` | Implemented | Confirms HomeScout product identity, React frontend direction, API-first architecture, and planned Foundry Agent Service target. |
| Listing comparison | `POST /api/comparison` | Implemented | Deterministic side-by-side of 2–4 `Listing`s via `IListingComparisonService`: price per ft²/m² (Buy), indicative monthly running cost, "what's missing?" completeness score + missing-info list, and descriptive cross-listing highlights (no safe/unsafe verdict). Invalid input → 400 ProblemDetails; typed client `CompareListingsAsync`. Replaces the old `/api/comparison/sample` placeholder. |
| Listing capture (extract) | `POST /api/listings/extract` | Implemented (text layer) | Multipart PDF upload (1–4 docs of one property) → `ListingExtractionResult`: a draft `Listing` + per-field provenance/confidence, for the user to confirm. Deterministic text layer (`PdfPigDocumentReader` + `IListingFactParser`) — reads the labelled fields/spec block across Rightmove/Zoopla/OpenRent layouts; never guesses (absent facts become notes). Vision + register cross-check are later slices. 400 ProblemDetails on non-PDF/empty. Plan: [[Listing Capture — PDF Extraction Pipeline — Design]]. |
| Base rate context | `GET /api/mortgage/base-rate` | Implemented | Bank of England base rate for orientation only (not a product rate). Live BoE fetch via `IBaseRateProvider`, ~1-day cache, resilient fallback; always returns 200 with a `Live`/`Cache`/`Fallback` provenance. |
| Mortgage estimate | `POST /api/mortgage/estimate` | Implemented | Deterministic amortisation via `IMortgageCostEstimator` (repayment + interest-only, +3% stress). Returns monthly payment, totals, assumptions, and a not-mortgage-advice caveat; invalid input → 400 ProblemDetails. |
| Copilot | `POST /api/copilot/ask` | Implemented (agent live-pending) | Natural-language question → grounded `CopilotAnswer` via `IHomeScoutAgentGateway` (Foundry agent calls the tools). 400 on empty message; **503 until Foundry is provisioned/configured**. Typed client `AskCopilotAsync`. |

## Planned HomeScout Tool Endpoints

These may become API routes, internal services, Foundry Agent Service tools, or backend tool wrappers depending on the implementation step.

| Tool | Input | Output | Status |
| --- | --- | --- | --- |
| Crime summary | postcode or lat/lon | crime counts, categories, recent trend caveats | Planned |
| Amenities lookup | postcode or lat/lon, radius | nearby amenities grouped by type | Planned |
| School context | postcode or lat/lon | nearby schools and key public metrics | Planned |
| Area nearby evidence | postcode or lat/lon, radius | named nearby places grouped by category, distance, source, freshness, provenance, and missing-data notes | Planned; contract direction in [[Area Evidence Map]] |
| Price context | postcode, district, or property details | sold-price context and local trend caveats | Planned |
| Ownership cost estimate (full) | price, deposit, rate, term, fees, council tax, insurance | monthly estimate and assumptions | Planned (extends the implemented mortgage estimate) |
| Area comparison | multiple locations | structured comparison report | Planned |
| Comparison draft | property inputs, buyer priorities, assumptions | structured comparison draft | Planned |
| User case-file retrieval | comparison/session id, query | cited uploaded-document evidence | Planned |
| Curated KB retrieval | query, source category | HomeScout-authored guidance with source metadata | Planned |

## Planned External Integrations

| Source | Purpose | Notes |
| --- | --- | --- |
| Police.uk API | Street-level crime data | Public API for England and Wales crime data. |
| HM Land Registry Open Data | Price Paid Data and house price context | Public datasets; may need ingestion/caching rather than direct per-request calls. |
| GOV.UK school performance data | School comparison context | Data access needs validation during implementation. |
| OpenStreetMap Overpass API | Amenities and local points of interest | Use carefully with rate limits and cache results. |
| TfL Unified API | London commute and transport context | Useful for London-focused MVP. |
| GOV.UK flood-risk services | Flood-risk context | Use as contextual area/property due-diligence information, not a definitive risk guarantee. |

## Data Flow Direction

```text
User prompt/upload
  -> React workspace
  -> HomeScout API service
  -> agent gateway and HomeScout tool services
  -> Microsoft Foundry Agent Service and/or public-data integrations
  -> structured evidence
  -> streamed assistant response/report
  -> saved comparison session
```

The frontend should call API endpoints. It should not create agents directly. See [[API-First Foundry Agents]].

## Safety Requirements

- Return assumptions with cost estimates.
- Label public data as contextual, not definitive.
- Do not present crime data as a simple safety score.
- Do not provide regulated mortgage advice.

See [[Overview]] and [[Coding Conventions]].
