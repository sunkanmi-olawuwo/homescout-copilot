using System.Text.Json;
using System.Text.Json.Serialization;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.Shared.Test;

// Locks the wire contract: DTOs must serialize to the camelCase property names the
// React frontend and the typed API client depend on.
public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // The API emits enums as strings (see Program.cs); the wire-contract tests must match.
    private static readonly JsonSerializerOptions WebEnumOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Test]
    public void HomeScoutStatus_serializes_with_camelCase_properties()
    {
        var json = JsonSerializer.Serialize(
            new HomeScoutStatus("HomeScout Copilot", "React", "API-first", "Foundry"),
            WebOptions);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("product").GetString(), Is.EqualTo("HomeScout Copilot"));
            Assert.That(root.GetProperty("frontend").GetString(), Is.EqualTo("React"));
            Assert.That(root.GetProperty("architecture").GetString(), Is.EqualTo("API-first"));
            Assert.That(root.GetProperty("agentPlatform").GetString(), Is.EqualTo("Foundry"));
        });
    }

    [Test]
    public void Listing_serializes_with_camelCase_and_string_enums()
    {
        var listing = new Listing(
            Label: "2-bed flat, Greenwich",
            Mode: ListingMode.Buy,
            Postcode: "SE10 9NF",
            Price: 525_000m,
            Bedrooms: 2,
            FloorArea: 68m,
            AreaUnit: FloorAreaUnit.SquareMetres,
            Tenure: PropertyTenure.Leasehold);

        var json = JsonSerializer.Serialize(listing, WebEnumOptions);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("label").GetString(), Is.EqualTo("2-bed flat, Greenwich"));
            Assert.That(root.GetProperty("mode").GetString(), Is.EqualTo("Buy"));
            Assert.That(root.GetProperty("areaUnit").GetString(), Is.EqualTo("SquareMetres"));
            Assert.That(root.GetProperty("tenure").GetString(), Is.EqualTo("Leasehold"));
            Assert.That(root.GetProperty("price").GetDecimal(), Is.EqualTo(525_000m));
        });
    }

    [Test]
    public void ComparisonRequest_round_trips_with_string_enums()
    {
        var original = new ComparisonRequest([
            new Listing("A", ListingMode.Buy, "SE10 9NF", Price: 525_000m),
            new Listing("B", ListingMode.Rent, "CR0 6BE", MonthlyRent: 1_650m, Furnishing: FurnishingState.Unfurnished),
        ]);

        var round = JsonSerializer.Deserialize<ComparisonRequest>(
            JsonSerializer.Serialize(original, WebEnumOptions), WebEnumOptions);

        Assert.That(round, Is.Not.Null);
        Assert.That(round!.Listings, Is.Not.Null);
        var listings = round.Listings!;
        Assert.Multiple(() =>
        {
            Assert.That(listings, Has.Count.EqualTo(2));
            Assert.That(listings[0].Mode, Is.EqualTo(ListingMode.Buy));
            Assert.That(listings[1].Furnishing, Is.EqualTo(FurnishingState.Unfurnished));
        });
    }
}
