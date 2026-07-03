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
| Foundry provisioning (azd + bicep) | Authored | `azure.yaml` + `infra/` bicep: Foundry account (AIServices) → chat model deployment → project → RBAC (Foundry User). Compiles (`az bicep build`, `infra-ci.yml`); not yet `azd up`-verified. Cosmos/Search/DocIntelligence deferred. |
| Copilot agent gateway | Partial | `IHomeScoutAgentGateway` + `CopilotRequest`/`CopilotAnswer`; `HomeScoutAgentTools` (estimator + base rate as `AIFunction`s, verified offline). `FoundryAgentGateway` (Microsoft Agent Framework, `AIProjectClient.AsAIAgent`) **compiles**; live `[Category("External")]` test skips offline, runs against real Foundry once provisioned (`azd provision`). Not yet live-verified. Endpoint/DI wiring pending (Slice 4). |
| Mortgage cost estimator | Implemented | `IMortgageCostEstimator` / `POST /api/mortgage/estimate`: deterministic amortisation (repayment + interest-only, +3% stress, LTV), FluentResults validation → ProblemDetails, typed client. Unit tests + `MortgageEstimate.feature` BDD. Mortgage-only MVP; explainable, not mortgage advice. |
| Base rate context provider | Implemented | `IBaseRateProvider` / `GET /api/mortgage/base-rate`: live Bank of England fetch (series `IUDBEDR`), ~1-day cache, resilient fallback (never throws). Orientation only, not a mortgage product rate. Offline unit + endpoint tests in the PR gate; **live fetch verified end-to-end** and kept verified by the nightly `external-checks` workflow. |
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
