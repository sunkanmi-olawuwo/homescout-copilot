using System.Globalization;
using FluentResults;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Deterministic, offline side-by-side comparison of user-entered listings — the decision-pack
/// spine. For each listing it derives price per ft²/m² (Buy), an indicative monthly running cost,
/// and a "what's missing?" completeness score, plus descriptive cross-listing highlights. It never
/// ranks an area as safe/unsafe or recommends a product, and does not verify listing details.
/// Expected failures are FluentResults, not exceptions.
/// </summary>
public interface IListingComparisonService
{
    Result<ComparisonResult> Compare(ComparisonRequest request);
}

public sealed class ListingComparisonService : IListingComparisonService
{
    private const int MinListings = 2;
    private const int MaxListings = 4;
    private const decimal SquareFeetPerSquareMetre = 10.7639m;
    private const int MonthsPerYear = 12;

    private static readonly IReadOnlyList<string> BaseCaveats =
    [
        "This compares the facts you entered — not property, mortgage, or tenancy advice.",
        "HomeScout doesn't verify listing details; confirm them with the agent or seller.",
    ];

    private static readonly IReadOnlyList<string> BaseAssumptions =
    [
        "Price per area uses 1 m² = 10.7639 ft².",
        "Indicative monthly cost is a running-cost figure (rent/council tax/service charge/bills); "
        + "it excludes the mortgage payment, which needs your financing inputs (see the estimator).",
        "Completeness scores which key facts are present for each listing — it is not a quality score.",
    ];

    public Result<ComparisonResult> Compare(ComparisonRequest request)
    {
        // A missing/empty body deserialises to null Listings — coalesce so validation fails cleanly.
        var listings = request.Listings ?? [];
        var validation = Validate(listings);
        if (validation.IsFailed)
        {
            return validation;
        }

        var analysed = listings.Select(Analyse).ToList();
        return Result.Ok(new ComparisonResult(analysed, Highlights(analysed), BaseAssumptions, BaseCaveats));
    }

    private static Result Validate(IReadOnlyList<Listing> listings)
    {
        if (listings.Count < MinListings)
        {
            return Result.Fail($"Provide at least {MinListings} listings to compare.");
        }

        if (listings.Count > MaxListings)
        {
            return Result.Fail($"Compare at most {MaxListings} listings at once.");
        }

        var errors = new List<string>();
        for (var i = 0; i < listings.Count; i++)
        {
            ValidateListing(listings[i], i, errors);
        }

        return errors.Count == 0 ? Result.Ok() : Result.Fail(errors);
    }

    private static void ValidateListing(Listing l, int index, List<string> errors)
    {
        var who = string.IsNullOrWhiteSpace(l.Label) ? $"Listing {index + 1}" : l.Label;
        if (string.IsNullOrWhiteSpace(l.Label))
        {
            errors.Add($"Listing {index + 1}: a label is required.");
        }

        if (string.IsNullOrWhiteSpace(l.Postcode))
        {
            errors.Add($"{who}: a postcode is required.");
        }

        if (l.Mode == ListingMode.Buy && l.Price is null or <= 0)
        {
            errors.Add($"{who}: a Buy listing needs an asking price greater than zero.");
        }

        if (l.Mode == ListingMode.Rent && l.MonthlyRent is null or <= 0)
        {
            errors.Add($"{who}: a Rent listing needs a monthly rent greater than zero.");
        }

        foreach (var (name, value) in MoneyFacts(l))
        {
            if (value is < 0)
            {
                errors.Add($"{who}: {name} cannot be negative.");
            }
        }

        if (l.Bedrooms is < 0)
        {
            errors.Add($"{who}: bedrooms cannot be negative.");
        }

        if ((l.FloorArea is not null) ^ (l.AreaUnit is not null))
        {
            errors.Add($"{who}: floor area and its unit must be provided together.");
        }
    }

    private static ListingComparison Analyse(Listing l)
    {
        var (perSqFt, perSqM) = PricePerArea(l);
        var indicative = IndicativeMonthlyCost(l);
        var keyFacts = KeyFacts(l);
        var present = keyFacts.Count(f => f.Present);
        var completeness = keyFacts.Count == 0 ? 0 : (int)Math.Round(100m * present / keyFacts.Count);
        var missing = keyFacts.Where(f => !f.Present).Select(f => f.MissingPrompt).ToList();
        return new ListingComparison(l, perSqFt, perSqM, indicative, completeness, missing, ListingNotes(l, indicative));
    }

    // Price per area is only meaningful for a Buy listing with a price and a floor area.
    private static (decimal? PerSqFt, decimal? PerSqM) PricePerArea(Listing l)
    {
        if (l.Mode != ListingMode.Buy || l.Price is not { } price || price <= 0
            || l.FloorArea is not { } area || area <= 0 || l.AreaUnit is not { } unit)
        {
            return (null, null);
        }

        var sqm = unit == FloorAreaUnit.SquareMetres ? area : area / SquareFeetPerSquareMetre;
        var sqft = unit == FloorAreaUnit.SquareFeet ? area : area * SquareFeetPerSquareMetre;
        return (Round(price / sqft), Round(price / sqm));
    }

    // A coarse running cost — NOT the estimator output. Rent always has one; Buy only if a running
    // cost was supplied (council tax / service charge / bills), otherwise null (unknown).
    private static decimal? IndicativeMonthlyCost(Listing l)
    {
        var councilTax = l.MonthlyCouncilTax ?? 0m;
        var bills = l.EstimatedMonthlyBills ?? 0m;
        if (l.Mode == ListingMode.Rent)
        {
            return Round((l.MonthlyRent ?? 0m) + councilTax + bills);
        }

        if (l.MonthlyCouncilTax is null && l.AnnualServiceCharge is null && l.EstimatedMonthlyBills is null)
        {
            return null;
        }

        return Round(councilTax + (l.AnnualServiceCharge ?? 0m) / MonthsPerYear + bills);
    }

    // The mode-specific set of key facts scored by CompletenessPercent, each with the prompt shown
    // when it is missing.
    private static IReadOnlyList<(bool Present, string MissingPrompt)> KeyFacts(Listing l)
    {
        var facts = new List<(bool, string)>
        {
            (l.Bedrooms is not null, "Number of bedrooms"),
            (l.FloorArea is not null, "Floor area (enables price per ft²/m²)"),
            (!string.IsNullOrWhiteSpace(l.EpcRating), "EPC rating"),
            (l.MonthlyCouncilTax is not null, "Monthly council tax"),
            (l.EstimatedMonthlyBills is not null, "Estimated monthly bills"),
        };

        if (l.Mode == ListingMode.Buy)
        {
            facts.Add((l.Price is not null, "Asking price"));
            facts.Add((l.Tenure is not null, "Tenure (freehold/leasehold)"));
        }
        else
        {
            facts.Add((l.MonthlyRent is not null, "Monthly rent"));
            facts.Add((l.Furnishing is not null, "Furnishing state"));
        }

        return facts;
    }

    private static IReadOnlyList<string> ListingNotes(Listing l, decimal? indicative)
    {
        var notes = new List<string>
        {
            l.Mode == ListingMode.Rent
                ? "Indicative monthly cost = rent + council tax + bills (your figures)."
                : indicative is null
                    ? "No running-cost figures yet — add council tax, service charge or bills for an indicative monthly cost."
                    : "Indicative monthly cost is running costs only (council tax + service charge + bills); it excludes the mortgage.",
        };

        if (l.Tenure == PropertyTenure.Leasehold && l.AnnualServiceCharge is null)
        {
            notes.Add("Leasehold: ask for the annual service charge and ground rent.");
        }

        return notes;
    }

    // Descriptive cross-listing callouts only — never "best", never a safe/unsafe verdict.
    private static IReadOnlyList<string> Highlights(IReadOnlyList<ListingComparison> listings)
    {
        var highlights = new List<string>();

        var byPerFt = listings.Where(x => x.PricePerSquareFoot is not null).ToList();
        if (byPerFt.Count > 1)
        {
            var cheapest = byPerFt.OrderBy(x => x.PricePerSquareFoot).First();
            highlights.Add($"Lowest price per ft²: {cheapest.Listing.Label} at {Money2(cheapest.PricePerSquareFoot!.Value)}/ft².");
        }

        var byMonthly = listings.Where(x => x.IndicativeMonthlyCost is not null).ToList();
        if (byMonthly.Count > 1)
        {
            var cheapest = byMonthly.OrderBy(x => x.IndicativeMonthlyCost).First();
            highlights.Add($"Lowest indicative monthly cost: {cheapest.Listing.Label} at {Money0(cheapest.IndicativeMonthlyCost!.Value)}/month.");
        }

        var mostComplete = listings.OrderByDescending(x => x.CompletenessPercent).First();
        highlights.Add($"Most complete listing: {mostComplete.Listing.Label} ({mostComplete.CompletenessPercent}% of key facts).");

        var missingArea = listings.Where(x => x.Listing.FloorArea is null).Select(x => x.Listing.Label).ToList();
        if (missingArea.Count > 0 && missingArea.Count < listings.Count)
        {
            highlights.Add($"Floor area is missing for {string.Join(", ", missingArea)}, so price per area couldn't be compared for them.");
        }

        return highlights;
    }

    private static IEnumerable<(string Name, decimal? Value)> MoneyFacts(Listing l) =>
    [
        ("asking price", l.Price),
        ("monthly rent", l.MonthlyRent),
        ("floor area", l.FloorArea),
        ("monthly council tax", l.MonthlyCouncilTax),
        ("annual service charge", l.AnnualServiceCharge),
        ("estimated monthly bills", l.EstimatedMonthlyBills),
    ];

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Money0(decimal value) => value.ToString("C0", CultureInfo.GetCultureInfo("en-GB"));

    private static string Money2(decimal value) => value.ToString("C2", CultureInfo.GetCultureInfo("en-GB"));
}
