# Endpoint Summary

This page tracks routes, external API integrations, and data flow.

## Current Internal Endpoints

The project currently has the first HomeScout-shaped API endpoints plus Aspire default health endpoints.

| Area | Endpoint | Status | Purpose |
| --- | --- | --- | --- |
| API health | `/health` | Scaffolded | Health check used by Aspire. |
| API liveness | `/alive` | Scaffolded | Liveness check from Aspire service defaults. |
| Product status | `/api/status` | Implemented | Confirms HomeScout product identity, React frontend direction, API-first architecture, and planned Foundry Agent Service target. |
| Sample comparison | `/api/comparison/sample` | Implemented | Placeholder sample response for the first API-first comparison workflow. |
| Base rate context | `GET /api/mortgage/base-rate` | Implemented | Bank of England base rate for orientation only (not a product rate). Live BoE fetch via `IBaseRateProvider`, ~1-day cache, resilient fallback; always returns 200 with a `Live`/`Cache`/`Fallback` provenance. |

## Planned HomeScout Tool Endpoints

These may become API routes, internal services, Foundry Agent Service tools, or backend tool wrappers depending on the implementation step.

| Tool | Input | Output | Status |
| --- | --- | --- | --- |
| Crime summary | postcode or lat/lon | crime counts, categories, recent trend caveats | Planned |
| Amenities lookup | postcode or lat/lon, radius | nearby amenities grouped by type | Planned |
| School context | postcode or lat/lon | nearby schools and key public metrics | Planned |
| Price context | postcode, district, or property details | sold-price context and local trend caveats | Planned |
| Mortgage estimate (`POST /api/mortgage/estimate`) | price, deposit, rate, term, repayment type | monthly repayment, total interest, +3% stress, assumptions, caveats | Designed — mortgage-only MVP ([backend plan](__plans/03-backend/cost-estimator-mortgage-plan.md)) |
| Ownership cost estimate (full) | price, deposit, rate, term, fees, council tax, insurance | monthly estimate and assumptions | Planned |
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
