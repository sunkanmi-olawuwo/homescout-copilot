using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Locks the deterministic listing comparison: validation, price-per-area normalisation, indicative
// monthly cost, the "what's missing?" completeness score, and descriptive cross-listing highlights.
// Not property/mortgage/tenancy advice — no safe/unsafe verdict.
[TestFixture]
public class ListingComparisonServiceTests
{
    private readonly ListingComparisonService _service = new();

    private static Listing Buy(string label, decimal price, decimal? area = null, FloorAreaUnit? unit = null)
        => new(label, ListingMode.Buy, "SE10 9NF", Price: price, FloorArea: area, AreaUnit: unit);

    private static ComparisonResult Compare(params Listing[] listings)
    {
        var result = new ListingComparisonService().Compare(new ComparisonRequest(listings));
        Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors.Select(e => e.Message)));
        return result.Value;
    }

    [Test]
    public void Fewer_than_two_listings_fails()
    {
        var result = _service.Compare(new ComparisonRequest([Buy("only", 500_000m)]));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("at least 2"));
    }

    [Test]
    public void More_than_four_listings_fails()
    {
        var many = Enumerable.Range(1, 5).Select(i => Buy($"l{i}", 400_000m)).ToArray();

        var result = _service.Compare(new ComparisonRequest(many));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("at most 4"));
    }

    [Test]
    public void Buy_listing_without_a_price_fails()
    {
        var result = _service.Compare(new ComparisonRequest([
            new Listing("no price", ListingMode.Buy, "SE10 9NF"),
            Buy("ok", 400_000m),
        ]));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("asking price")), Is.True);
    }

    [Test]
    public void Rent_listing_without_rent_fails()
    {
        var result = _service.Compare(new ComparisonRequest([
            new Listing("no rent", ListingMode.Rent, "SE10 9NF"),
            new Listing("ok", ListingMode.Rent, "CR0 6BE", MonthlyRent: 1_500m),
        ]));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("monthly rent")), Is.True);
    }

    [Test]
    public void Negative_fact_fails()
    {
        var result = _service.Compare(new ComparisonRequest([
            new Listing("bad", ListingMode.Buy, "SE10 9NF", Price: 400_000m, MonthlyCouncilTax: -10m),
            Buy("ok", 500_000m),
        ]));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("cannot be negative")), Is.True);
    }

    [Test]
    public void Floor_area_requires_a_unit()
    {
        var result = _service.Compare(new ComparisonRequest([
            new Listing("no unit", ListingMode.Buy, "SE10 9NF", Price: 400_000m, FloorArea: 70m),
            Buy("ok", 500_000m),
        ]));

        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("floor area and its unit")), Is.True);
    }

    [Test]
    public void Price_per_area_is_computed_and_normalised_across_units()
    {
        // Same £400,000 at 100 m² vs the equivalent 1076.39 ft² must give the same £/ft² and £/m².
        var result = Compare(
            Buy("metres", 400_000m, 100m, FloorAreaUnit.SquareMetres),
            Buy("feet", 400_000m, 1076.39m, FloorAreaUnit.SquareFeet));

        var metres = result.Listings[0];
        var feet = result.Listings[1];
        Assert.Multiple(() =>
        {
            Assert.That(metres.PricePerSquareMetre, Is.EqualTo(4_000m)); // 400000 / 100
            Assert.That(metres.PricePerSquareFoot, Is.EqualTo(371.61m).Within(0.05m)); // 400000 / 1076.39
            Assert.That(feet.PricePerSquareMetre, Is.EqualTo(metres.PricePerSquareMetre!.Value).Within(0.5m));
            Assert.That(feet.PricePerSquareFoot, Is.EqualTo(metres.PricePerSquareFoot!.Value).Within(0.05m));
        });
    }

    [Test]
    public void Rent_indicative_monthly_cost_sums_rent_council_tax_and_bills()
    {
        var result = Compare(
            new Listing("a", ListingMode.Rent, "SE10 9NF", MonthlyRent: 1_500m, MonthlyCouncilTax: 150m, EstimatedMonthlyBills: 200m),
            new Listing("b", ListingMode.Rent, "CR0 6BE", MonthlyRent: 1_300m));

        Assert.That(result.Listings[0].IndicativeMonthlyCost, Is.EqualTo(1_850m));
        Assert.That(result.Listings[1].IndicativeMonthlyCost, Is.EqualTo(1_300m)); // rent only
    }

    [Test]
    public void Buy_indicative_monthly_cost_is_running_costs_only_and_null_when_absent()
    {
        var result = Compare(
            new Listing("costed", ListingMode.Buy, "SE10 9NF", Price: 500_000m, MonthlyCouncilTax: 180m, AnnualServiceCharge: 2_400m),
            Buy("bare", 400_000m));

        Assert.That(result.Listings[0].IndicativeMonthlyCost, Is.EqualTo(380m)); // 180 + 2400/12
        Assert.That(result.Listings[1].IndicativeMonthlyCost, Is.Null); // no running-cost facts
    }

    [Test]
    public void Completeness_reflects_present_key_facts_and_lists_the_missing_ones()
    {
        var full = new Listing("full", ListingMode.Buy, "SE10 9NF", Price: 500_000m, Bedrooms: 2,
            FloorArea: 70m, AreaUnit: FloorAreaUnit.SquareMetres, Tenure: PropertyTenure.Freehold,
            EpcRating: "C", MonthlyCouncilTax: 160m, EstimatedMonthlyBills: 150m);
        var sparse = Buy("sparse", 400_000m);

        var result = Compare(full, sparse);

        Assert.Multiple(() =>
        {
            Assert.That(result.Listings[0].CompletenessPercent, Is.EqualTo(100));
            Assert.That(result.Listings[0].MissingInformation, Is.Empty);
            Assert.That(result.Listings[1].CompletenessPercent, Is.LessThan(100));
            Assert.That(result.Listings[1].MissingInformation, Does.Contain("EPC rating"));
        });
    }

    [Test]
    public void Highlights_are_descriptive_and_never_a_safe_unsafe_verdict()
    {
        var result = Compare(
            Buy("cheaper per ft", 300_000m, 100m, FloorAreaUnit.SquareMetres),
            Buy("dearer per ft", 600_000m, 100m, FloorAreaUnit.SquareMetres));

        Assert.Multiple(() =>
        {
            Assert.That(result.Highlights.Any(h => h.Contains("Lowest price per ft²") && h.Contains("cheaper per ft")), Is.True);
            Assert.That(result.Highlights.Any(h => h.Contains("Most complete listing")), Is.True);
            Assert.That(result.Caveats.Any(c => c.Contains("not property, mortgage, or tenancy advice")), Is.True);
            // No safe/unsafe language anywhere in the output.
            var all = string.Join(" ", result.Highlights.Concat(result.Caveats).Concat(result.Assumptions));
            Assert.That(all.ToLowerInvariant(), Does.Not.Contain("safe"));
        });
    }

    [Test]
    public void Highlight_notes_when_floor_area_missing_blocks_price_per_area_comparison()
    {
        var result = Compare(
            Buy("has area", 400_000m, 80m, FloorAreaUnit.SquareMetres),
            Buy("no area", 420_000m));

        Assert.That(result.Highlights.Any(h => h.Contains("Floor area is missing") && h.Contains("no area")), Is.True);
    }
}
