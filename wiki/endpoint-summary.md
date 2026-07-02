# Endpoint Summary

This page tracks routes, external API integrations, and data flow.

## Current Internal Endpoints

The project currently has starter-template endpoints only.

| Area | Endpoint | Status | Purpose |
| --- | --- | --- | --- |
| API health | `/health` | Scaffolded | Health check used by Aspire. |
| Web health | `/health` | Scaffolded | Health check used by Aspire. |
| Weather sample | Template route | Scaffolded | Starter sample; expected to be replaced. |

## Planned HomeScout Tool Endpoints

These may become API routes, internal services, or Microsoft Agent Framework tools depending on the course implementation step.

| Tool | Input | Output | Status |
| --- | --- | --- | --- |
| Crime summary | postcode or lat/lon | crime counts, categories, recent trend caveats | Planned |
| Amenities lookup | postcode or lat/lon, radius | nearby amenities grouped by type | Planned |
| School context | postcode or lat/lon | nearby schools and key public metrics | Planned |
| Price context | postcode, district, or property details | sold-price context and local trend caveats | Planned |
| Ownership cost estimate | price, deposit, rate, term, fees | monthly estimate and assumptions | Planned |
| Area comparison | multiple locations | structured comparison report | Planned |

## Planned External Integrations

| Source | Purpose | Notes |
| --- | --- | --- |
| Police.uk API | Street-level crime data | Public API for England and Wales crime data. |
| HM Land Registry Open Data | Price Paid Data and house price context | Public datasets; may need ingestion/caching rather than direct per-request calls. |
| GOV.UK school performance data | School comparison context | Data access needs validation during implementation. |
| OpenStreetMap Overpass API | Amenities and local points of interest | Use carefully with rate limits and cache results. |
| TfL Unified API | London commute and transport context | Useful for London-focused MVP. |

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

