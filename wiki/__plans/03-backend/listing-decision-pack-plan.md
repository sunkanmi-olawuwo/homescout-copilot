# Listing Model + Comparison Spine — Design

**Status:** In progress — Slice 1 (backend, stateless). The MVP priority from
[[Market Landscape And Product Lessons]]: HomeScout's wedge is the **evidence-backed decision
layer between finding a listing and committing to it**. This slice introduces the `Listing`
domain model and a real side-by-side comparison, replacing the `/api/comparison/sample`
placeholder.

**Owning context:** [[Phased Learning And Build Plan]] Phase 2 (API Contract And Deterministic
Tooling) — supersedes the vague `POST /api/comparisons/draft` placeholder with the concrete
`POST /api/comparison`. Tracked under "Decision-Pack MVP" in [[Feature Coverage]].

## Why

The comparison workspace has only ever had `GET /api/comparison/sample` — a two-string
placeholder. Almost every planned decision-pack feature (side-by-side, "what's missing?" score,
price per ft², area evidence) hangs off a real `Listing`. This slice builds that spine so those
features become buildable, and gives the frontend a real endpoint to compare against.

Not property, mortgage, or tenancy advice: the comparison reflects the facts the user entered,
adds derived metrics and a completeness view, and never ranks an area as safe/unsafe or
recommends a product. HomeScout does not verify listing details.

## Scope

**Slice 1 (this plan): stateless comparison.** `POST /api/comparison` takes the listings' facts
directly and returns the side-by-side. No persistence — proves the model + comparison shape
offline (seam-first) with no new DB coupling, and is fully unit/contract-testable.

**Later slices (not this plan):** per-user persistence of listings + shortlists (layers onto the
existing Postgres session/user-directory infra), paste-a-link / upload fact extraction, and the
React capture + compare UI (a parallel frontend track).

## Design

### Contracts — `Shared/Contracts/ListingContracts.cs`

```csharp
public enum ListingMode { Buy, Rent }
public enum PropertyTenure { Freehold, Leasehold, ShareOfFreehold }
public enum FloorAreaUnit { SquareFeet, SquareMetres }
public enum FurnishingState { Furnished, PartFurnished, Unfurnished }

// A user-confirmed listing. Mode decides which headline figure applies (Price vs MonthlyRent);
// everything else is optional so a partial listing still compares (and is scored for completeness).
public record Listing(
    string Label,
    ListingMode Mode,
    string Postcode,
    decimal? Price = null,                 // Buy: asking price
    decimal? MonthlyRent = null,           // Rent: monthly rent
    int? Bedrooms = null,
    decimal? FloorArea = null,
    FloorAreaUnit? AreaUnit = null,
    PropertyTenure? Tenure = null,         // Buy
    string? EpcRating = null,              // A–G
    decimal? MonthlyCouncilTax = null,
    decimal? AnnualServiceCharge = null,   // Buy, typically leasehold
    FurnishingState? Furnishing = null,    // Rent
    decimal? EstimatedMonthlyBills = null,
    string? SourceUrl = null,              // deep link back to the source listing
    string? Notes = null);

public record ComparisonRequest(IReadOnlyList<Listing> Listings);

// Per-listing computed view. Price-per-area is from Price (Buy) only; IndicativeMonthlyCost is a
// coarse running-cost figure (see below), distinct from the precise mortgage/rental estimators.
public record ListingComparison(
    Listing Listing,
    decimal? PricePerSquareFoot,
    decimal? PricePerSquareMetre,
    decimal? IndicativeMonthlyCost,
    int CompletenessPercent,                       // 0–100
    IReadOnlyList<string> MissingInformation,      // absent key facts, phrased as what to ask for
    IReadOnlyList<string> Notes);

public record ComparisonResult(
    IReadOnlyList<ListingComparison> Listings,
    IReadOnlyList<string> Highlights,              // cross-listing, descriptive only
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
```

### Service — `API.Service/Comparison/ListingComparisonService.cs`

`IListingComparisonService.Compare(ComparisonRequest) : Result<ComparisonResult>`. Deterministic,
offline, no advice. Expected failures are FluentResults → ProblemDetails (mirrors the estimators).

**Validation** (→ `Result.Fail`):
- 2–4 listings (need at least two to compare; cap keeps the view readable).
- Each listing: non-empty `Label` and `Postcode`; `Mode`-appropriate headline present
  (`Price` for Buy, `MonthlyRent` for Rent) and > 0; any provided numeric fact ≥ 0;
  `FloorArea` requires `AreaUnit` (and vice versa).

**Derived per listing:**
- **Price per area** (Buy with `Price` + `FloorArea` + `AreaUnit`): normalise to both units using
  1 m² = 10.7639 ft². Null for Rent or when area is missing.
- **IndicativeMonthlyCost** — a coarse running cost, clearly *not* the estimator output:
  - Rent: `MonthlyRent + (MonthlyCouncilTax ?? 0) + (EstimatedMonthlyBills ?? 0)`.
  - Buy: `(MonthlyCouncilTax ?? 0) + (AnnualServiceCharge ?? 0)/12 + (EstimatedMonthlyBills ?? 0)`
    — running cost only; the mortgage payment needs financing inputs and stays with
    `POST /api/mortgage/estimate`.
- **CompletenessPercent + MissingInformation** — over a mode-specific set of key facts (common:
  Bedrooms, FloorArea, EpcRating, MonthlyCouncilTax, EstimatedMonthlyBills; Buy adds Price, Tenure;
  Rent adds MonthlyRent, Furnishing). Percent = present/total; missing ones are listed as
  actionable prompts ("Ask the agent for the EPC rating").

**Highlights** (cross-listing, descriptive — never "best"/"safe"): lowest price per ft², lowest
indicative monthly cost, most complete listing; and a note when a shared fact (e.g. floor area) is
missing from some listings so a metric couldn't be compared.

**Caveats** (always): "This compares the facts you entered — not property, mortgage, or tenancy
advice. HomeScout doesn't verify listing details; confirm them with the agent or seller." No
safe/unsafe area verdict.

### Endpoint — `API/Features/Comparison/ComparisonEndpoints.cs`

Replace the placeholder with:

```
POST /api/comparison   (ComparisonRequest) -> ComparisonResult
```

Carter module → MediatR `CompareListingsCommand` → handler → `service.Compare(request).ToHttpResult()`.
Remove `GetComparisonSample*`, the `ComparisonSample` DTO, `IHomeScoutService.GetComparisonSample`,
and the client's `GetComparisonSampleAsync`; add a typed `CompareListingsAsync` to the client.

## Slice (mirrors the estimators)

1. **Contracts** — `Shared/Contracts/ListingContracts.cs` (+ remove `ComparisonSample`).
2. **Service** — `API.Service/Comparison/ListingComparisonService.cs`, registered in DI.
3. **REST endpoint** — `POST /api/comparison`; delete the sample query/handler.
4. **Typed client** — `CompareListingsAsync` on `HomeScoutApiClient` (drop the sample method).
5. **Tests** — unit tests for the service (validation, price-per-area, completeness, highlights);
   contract test for the route; contract-serialization round-trip for the new DTOs.

## Verification / Acceptance criteria

- [ ] `POST /api/comparison` returns a real side-by-side for 2–4 listings; `/api/comparison/sample`
      is gone (route + DTO + client + service method removed, tests updated).
- [ ] Each listing shows price-per-ft²/m² (Buy, when area present), an indicative monthly cost, a
      completeness percent, and an actionable missing-info list.
- [ ] Cross-listing highlights are descriptive; no safe/unsafe verdict; caveats always present.
- [ ] Invalid input (< 2 listings, missing headline, negative fact) → 400 ProblemDetails, not an
      exception (FluentResults).
- [ ] Deterministic + offline: unit + contract + serialization tests, all in the PR gate.
- [ ] Plan and code use identical names; `scripts/quality-gate.sh` clean (drift 0 fail).

## Related

[[Feature Coverage]] · [[Market Landscape And Product Lessons]] · [[Endpoint Summary]] ·
[Mortgage Cost Estimator](./cost-estimator-mortgage-plan.md) ·
[Rental Cost Estimator](./rental-cost-estimator-plan.md) · [[Area Evidence Map]] (depends on this
model's lat/long path).
