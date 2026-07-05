# Feature Coverage

This page tracks feature inventory across scaffolded, implemented, planned, and deferred work.

## Implemented

| Feature | Status | Notes |
| --- | --- | --- |
| Aspire solution scaffold | Implemented | Created from `aspire-starter`. |
| React frontend project | Implemented | Vite React project with initial HomeScout comparison workspace shell. |
| Layered backend | Implemented | `.API`/`.API.Service`/`.API.Client`/`.Shared`/`.Functional`. `/api/status` and `/api/comparison/sample` flow through `IHomeScoutService` (FluentResults) and map via `.ToHttpResult()`. HomeScout tools not yet implemented. |
| Test suite (NUnit + Reqnroll BDD) | Implemented | Per-project NUnit tests; API contract + Aspire integration + Reqnroll/Allure BDD; Functional mapper and Shared contract unit tests. |
| Frontend tests (Vitest + Playwright) | Implemented | Vitest + Testing Library component tests for the workspace shell/composer/evidence; Playwright chromium e2e smoke. Both in the CI quality gate. |
| Foundry provisioning (azd + bicep) | Authored | `azure.yaml` + `infra/` bicep: Foundry account (AIServices) → chat model deployment → project → RBAC (Foundry User). Compiles (`az bicep build`, `infra-ci.yml`); not yet `azd up`-verified. Cosmos/Search/DocIntelligence deferred. |
| Copilot (agent + endpoint) | Implemented | `POST /api/copilot/ask` → `IHomeScoutAgentGateway`; `HomeScoutAgentTools` (mortgage + rental estimators + base rate as `AIFunction`s; serves both buyers and renters, prompt `homescout.v3.md`); `FoundryAgentGateway` (Microsoft Agent Framework); DI registers it behind config (503 until Foundry configured); typed `AskCopilotAsync`; offline endpoint tests (fake gateway). **Live-verified 2026-07-05** against the Foundry `chat` (gpt-5-mini) deployment: the agent calls `estimate_mortgage`, carries multi-turn context, and rehydrates a session from PostgreSQL across a restart. |
| Mortgage cost estimator | Implemented | `IMortgageCostEstimator` / `POST /api/mortgage/estimate`: deterministic amortisation (repayment + interest-only, +3% stress, LTV), FluentResults validation → ProblemDetails, typed client. Unit tests + `MortgageEstimate.feature` BDD. Buyer MVP; explainable, not mortgage advice. |
| Rental cost estimator | Implemented | `IRentalCostEstimator` / `POST /api/rental/estimate`: deterministic true-monthly (rent + council tax + bills) and upfront/deposit cost; Tenant Fees Act 2019 caps (tenancy deposit 5 weeks, 6 at ≥£50k annual rent; holding deposit 1 week), FluentResults validation → ProblemDetails. Wired as the `estimate_rental_cost` agent tool → evidence panel. Unit tests. Renter analogue of the mortgage estimator; explainable, not tenancy advice. |
| Listing model + side-by-side comparison | Implemented (backend, stateless) | `Listing` domain model + `IListingComparisonService` / `POST /api/comparison`: deterministic offline side-by-side of 2–4 listings — price per ft²/m² (Buy), indicative monthly running cost, "what's missing?" completeness score + actionable missing-info list, descriptive cross-listing highlights (no safe/unsafe verdict). FluentResults validation → ProblemDetails; typed `CompareListingsAsync`. Replaces the `/api/comparison/sample` placeholder. Unit + contract + serialization tests. The decision-pack spine; per-user persistence + the React capture/compare UI are the next slices. Plan: [[Listing Model + Comparison Spine — Design]]. |
| Base rate context provider | Implemented | `IBaseRateProvider` / `GET /api/mortgage/base-rate`: live Bank of England fetch (series `IUDBEDR`), ~1-day cache, resilient fallback (never throws). Orientation only, not a mortgage product rate. Offline unit + endpoint tests in the PR gate; **live fetch verified end-to-end** and kept verified by the nightly `external-checks` workflow. |
| End-user authentication (Keycloak) | Partial | Keycloak/OIDC per [[Plan Divergence]] (Azure resource access stays Entra). Aspire-hosted Keycloak + committed `homescout` realm (`homescout-api` bearer + `homescout-web` PKCE clients); API validates JWTs (`AddKeycloakJwtBearer`, `homescout-api` audience), anonymous-capable (only per-user endpoints require a token); `app_users` directory resolves `(provider, subject)` → internal `UserId` via a race-safe upsert, with `OnTokenValidated` JIT capture; `GET /api/me`. Steps 1–3 **live-verified** (real Keycloak + Postgres). Remaining: per-user session association + history endpoints, anon→auth hand-off, frontend login. Plan: [[Keycloak Auth + Per-User History — Design]]. |
| Durable conversation-session store | Implemented | `ISessionStore` seam: `PostgresSessionStore` (Npgsql, `conversation_sessions` jsonb blob-by-id) + `NullSessionStore` (graceful in-memory-only default when no DB). `FoundryAgentGateway` is write-through (rehydrate on miss via `DeserializeSessionAsync`, persist after each turn via `SerializeSessionAsync`); sweeper + reset also purge the store. Aspire `AddPostgres().AddDatabase("sessions")`, config-gated. Testcontainers integration tests (`Category=Database`, run in the PR gate, self-skip without Docker); **live-verified end-to-end 2026-07-05** (session rehydrated from PostgreSQL across a simulated API restart against real Foundry). |
| Static analysis (local, advisory) | Implemented | `scripts/static-analysis.sh` — Lizard complexity (C#+TS), JetBrains InspectCode (.NET, `.slnx`), actionlint (workflows), ESLint complexity budget (frontend). Advisory: reports, never blocks. Advisory CI (`static-analysis.yml`) + quality-gate step; shared by all agents (Claude skill + `AGENTS.md`). Replaces the CodeQL scanning that went dormant on the private repo. See [[Static Analysis]]. |
| Wiki structure | Implemented | Canonical docs under `wiki/`. |

## Scaffolded But Not Product-Ready

| Feature | Status | Notes |
| --- | --- | --- |
| React comparison workspace shell | Scaffolded | Product layout exists, but generate/attach actions are placeholders. |

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

## Planned — Decision-Pack MVP (market-informed)

From the competitor/adjacent-product scan in [[Market Landscape And Product Lessons]]. HomeScout's
wedge is the **evidence-backed decision layer between finding a listing and committing to it** —
distinct from portals (discovery), data platforms (B2B reports), and rental marketplaces
(transactions). These extend the accelerator MVP; the cost estimators + evidence panel already exist.

| Feature | Status | Notes |
| --- | --- | --- |
| Listing capture (PDF upload → extract) | Implemented (text layer) | `POST /api/listings/extract`: multipart PDF upload → draft `Listing` + per-field provenance/confidence for the user to confirm. Deterministic **text layer** live (`PdfPigDocumentReader` word-level extraction + `IListingFactParser`), verified against real Rightmove/Zoopla/OpenRent PDFs — reads price/rent, beds, size+unit, tenure, EPC, council-tax band, furnishing, property type, outward postcode across all three layouts; never guesses (absent facts → notes). Vision + register cross-check + eval set are later slices per [[Listing Capture — PDF Extraction Pipeline — Design]]. Terms-safe (user provides the document; no scraping). |
| Structured listing facts (user-confirmed) | Implemented (model) | `Listing` record: label, mode (Buy/Rent), postcode, price/rent, beds, tenure, EPC, council tax, service charge, floor area (+unit), furnishing, bills, source URL, notes. See [[Listing Model + Comparison Spine — Design]]. |
| Side-by-side comparison | Implemented (backend) | `POST /api/comparison` via `IListingComparisonService` — the real workflow, replacing the `/api/comparison/sample` placeholder. React compare UI is the next frontend slice. |
| Missing-information checklist + "what's missing?" score | Implemented (backend) | Differentiator — `CompletenessPercent` + actionable `MissingInformation` per listing, over a mode-specific key-fact set. |
| Hidden/true-cost comparison | Partial | Mortgage + rental estimators shipped; the comparison now adds an **indicative** monthly running cost across listings. Wiring the precise estimators into the comparison is a refinement. |
| Price per square foot/metre | Implemented (backend) | `PricePerSquareFoot`/`PricePerSquareMetre` derived for Buy listings with floor area (normalised 1 m² = 10.7639 ft²). |
| Area evidence panel (commute, schools, amenities, flood/noise) | Planned | Per-listing, source-linked, caveated — no simplistic safe/unsafe labels (Crystal Roof). The map/list implementation direction lives in [[Area Evidence Map]]. |
| User preference capture + fit explanation | Planned | Commute, budget, schools, furnishing, outdoor space, risk tolerance → "why this may/may not fit you"; ties to the Memory course feature (OnTheMarket/Jitty). |
| Viewing / application / offer questions | Planned | "What to ask before viewing/applying/offering", generated from the pack. |
| Renter readiness / "before you apply" checklist | Planned | Documents, deposit, guarantor, affordability assumptions (Canopy). |
| Renter early-warning / fraud checklist | Planned | Verify agent/landlord, don't pay before viewing, deposit-scheme + permitted-fees check; links to official guidance, not tenancy advice (OpenRent/RentProfile). |
| Shareable / exportable decision pack | Planned | The artifact a user sends to a partner, parent, broker, or agent — the ChatGPT-defence. |

### Later (market-informed)

| Feature | Reason / source |
| --- | --- |
| Price-change / relisted-property tracking | Buyer/renter signal where allowed (Property Log). |
| Browser extension "Analyse with HomeScout" | Validation path; terms/affiliation clarity required. |
| Resident-review evidence | Evidence type raw listings can't replace (HomeViews); needs a licensed source. |
| Partner handoff / "next professional to speak to" | Broker, surveyor, conveyancer, relocation adviser — value-first, no premature lead-push (Konnect/Reallymoving/OneDome). |
| White-label agent/relocation/broker report | B2B mode (Sprift). |
| Data-provider partnerships (reports, comparables) | Partner rather than rebuild datasets. |

## Deferred

| Feature | Reason |
| --- | --- |
| Regulated mortgage product recommendations | Outside product safety boundary. |
| Definitive property valuation | Requires careful valuation model and liability controls. |
| Crime safety score | Too reductive; use contextual summaries instead. |
