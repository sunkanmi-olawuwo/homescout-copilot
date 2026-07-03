# Feature Coverage

This page tracks feature inventory across scaffolded, implemented, planned, and deferred work.

## Implemented

| Feature | Status | Notes |
| --- | --- | --- |
| Aspire solution scaffold | Implemented | Created from `aspire-starter`. |
| React frontend project | Implemented | Vite React project with initial HomeScout comparison workspace shell. |
| Layered backend | Implemented | `.API`/`.API.Service`/`.API.Client`/`.Shared.Application`/`.Functional`. `/api/status` and `/api/comparison/sample` flow through `IHomeScoutService` (FluentResults) and map via `.ToHttpResult()`. HomeScout tools not yet implemented. |
| Test suite (NUnit + Reqnroll BDD) | Implemented | Per-project NUnit tests; API contract + Aspire integration + Reqnroll/Allure BDD; Functional mapper and Shared contract unit tests. |
| Frontend tests (Vitest + Playwright) | Implemented | Vitest + Testing Library component tests for the workspace shell/composer/evidence; Playwright chromium e2e smoke. Both in the CI quality gate. |
| Base rate context provider | Implemented | `IBaseRateProvider` / `GET /api/mortgage/base-rate`: live Bank of England fetch (series `IUDBEDR`), ~1-day cache, resilient fallback (never throws). Orientation only, not a mortgage product rate. Unit + endpoint tests (no live network). |
| Wiki structure | Implemented | Canonical docs under `wiki/`. |

## Scaffolded But Not Product-Ready

| Feature | Status | Notes |
| --- | --- | --- |
| React comparison workspace shell | Scaffolded | Product layout exists, but generate/attach actions are placeholders. |
| Sample comparison API | Scaffolded | `/api/comparison/sample` exists as a placeholder, not the real comparison workflow. |

## Planned From Course Mapping

| Course Feature | HomeScout Feature | Status |
| --- | --- | --- |
| Course Blazor baseline | React property and area comparison workspace | Planned |
| Tool calls | Crime, amenities, school, price, cost tools | Planned |
| Reasoning | Explainable comparison notes and evidence trail | Planned |
| Streaming | Live report generation | Planned |
| Conversation history | Saved property searches and comparison sessions | Planned |
| Image generation | Optional report graphics or area summary visuals | Planned |
| Image/PDF input | Upload listings, EPCs, surveys, floorplans | Planned |
| User auth | Private user workspace | Planned |
| Memory | Buyer preferences and search priorities | Planned |
| Speech input | Spoken viewing notes and search criteria | Planned |
| RAG user case file | Private retrieval over listings, EPCs, surveys, floorplans, notes, messages, screenshots, photos, and preferences | Planned |
| Curated HomeScout knowledge base | Stable retrieval layer for homebuying explainers, terminology, assumptions, safety rules, and source guidance | Planned |

See [[Course Playlist Tracker]].

## Deferred

| Feature | Reason |
| --- | --- |
| Regulated mortgage product recommendations | Outside product safety boundary. |
| Definitive property valuation | Requires careful valuation model and liability controls. |
| Crime safety score | Too reductive; use contextual summaries instead. |
