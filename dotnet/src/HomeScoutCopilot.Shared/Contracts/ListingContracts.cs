namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>Whether a listing is for sale (Buy) or to let (Rent) — decides which headline figure
/// (<see cref="Listing.Price"/> vs <see cref="Listing.MonthlyRent"/>) applies.</summary>
public enum ListingMode
{
    Buy,
    Rent,
}

/// <summary>Ownership type for a Buy listing.</summary>
public enum PropertyTenure
{
    Freehold,
    Leasehold,
    ShareOfFreehold,
}

/// <summary>Unit the user gave the floor area in. Used to normalise price-per-area.</summary>
public enum FloorAreaUnit
{
    SquareFeet,
    SquareMetres,
}

/// <summary>Furnishing state for a Rent listing. <see cref="AtTenantChoice"/> covers the common
/// landlord-direct "furnished or unfurnished, tenant's choice" option.</summary>
public enum FurnishingState
{
    Furnished,
    PartFurnished,
    Unfurnished,
    AtTenantChoice,
}

/// <summary>UK council tax band. Listings give the band, not the monthly £ — the £ is derived from
/// the band plus the local authority's rates.</summary>
public enum CouncilTaxBand
{
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
}

/// <summary>How a Buy asking price is framed, so a "guide" or "offers over" figure is not treated as
/// firm.</summary>
public enum PriceQualifier
{
    Guide,
    OffersOver,
    OffersInRegionOf,
    FixedPrice,
    Poa,
}

/// <summary>
/// A user-confirmed property listing. <see cref="Mode"/> decides which headline figure applies
/// (<see cref="Price"/> for Buy, <see cref="MonthlyRent"/> for Rent); everything else is optional so
/// a partial listing still compares — the missing facts are surfaced by the completeness score.
/// HomeScout never sources these values; they are the user's own entries from the source listing.
/// </summary>
public record Listing(
    string Label,
    ListingMode Mode,
    string Postcode,
    decimal? Price = null,
    decimal? MonthlyRent = null,
    int? Bedrooms = null,
    decimal? FloorArea = null,
    FloorAreaUnit? AreaUnit = null,
    PropertyTenure? Tenure = null,
    string? EpcRating = null,
    decimal? MonthlyCouncilTax = null,
    decimal? AnnualServiceCharge = null,
    FurnishingState? Furnishing = null,
    decimal? EstimatedMonthlyBills = null,
    string? SourceUrl = null,
    string? Notes = null,
    // Added with the capture slice (see listing-capture-extraction-plan): all optional, so the
    // comparison spine is unaffected. Council tax is captured as a band (listings give the band,
    // not a monthly £); PriceQualifier flags a non-firm asking price; AddressLine carries the
    // street the description exposes, for precise geocoding when the portal hides the full postcode.
    CouncilTaxBand? CouncilTaxBand = null,
    string? PropertyType = null,
    int? Bathrooms = null,
    int? Receptions = null,
    PriceQualifier? PriceQualifier = null,
    string? AddressLine = null,
    // Resolved from the postcode by the register cross-check (postcodes.io), not user-entered. Unblocks
    // the area-evidence map; approximate (area centroid) when only an outward code is known.
    double? Latitude = null,
    double? Longitude = null);

/// <summary>Inputs to <c>POST /api/comparison</c>: the two to four listings to compare side by side.
/// Nullable because a client can POST a missing/empty body — the service validates it to a 400.</summary>
public record ComparisonRequest(IReadOnlyList<Listing>? Listings);

/// <summary>The computed view of a single listing within a comparison. Price-per-area is derived from
/// <see cref="Listing.Price"/> (Buy) only; <see cref="IndicativeMonthlyCost"/> is a coarse running-cost
/// figure, distinct from the precise mortgage/rental estimator outputs.</summary>
public record ListingComparison(
    Listing Listing,
    decimal? PricePerSquareFoot,
    decimal? PricePerSquareMetre,
    decimal? IndicativeMonthlyCost,
    int CompletenessPercent,
    IReadOnlyList<string> MissingInformation,
    IReadOnlyList<string> Notes);

/// <summary>Result of <c>POST /api/comparison</c>: each listing's computed view plus descriptive
/// cross-listing highlights. Highlights never rank an area as safe/unsafe or recommend a product;
/// caveats are always present. Not property, mortgage, or tenancy advice.</summary>
public record ComparisonResult(
    IReadOnlyList<ListingComparison> Listings,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Caveats);
