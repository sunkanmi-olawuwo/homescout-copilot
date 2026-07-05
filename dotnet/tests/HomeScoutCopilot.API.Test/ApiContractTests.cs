using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeScoutCopilot.API.Test;

// Fast, node-free contract tests: boot the API in-memory and assert the public
// response shape. These are the behaviour-lock through the relocation/layering
// phases — they must keep passing unedited while projects move and split.
[TestFixture]
public class ApiContractTests
{
    private WebApplicationFactory<ApiMarker> _factory = null!;

    [OneTimeSetUp]
    public void SetUp() => _factory = new WebApplicationFactory<ApiMarker>();

    [OneTimeTearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task Status_returns_ok_with_expected_shape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/status");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.That(root.GetProperty("product").GetString(), Is.EqualTo("HomeScout Copilot"));
        Assert.That(root.GetProperty("frontend").GetString(), Is.EqualTo("React"));
        Assert.That(root.GetProperty("architecture").GetString(), Is.EqualTo("API-first"));
        Assert.That(root.GetProperty("agentPlatform").GetString(), Is.Not.Null.And.Not.Empty);
    }

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Test]
    public async Task Comparison_returns_ok_with_per_listing_and_highlights()
    {
        var client = _factory.CreateClient();
        var request = new ComparisonRequest([
            new Listing("Greenwich flat", ListingMode.Buy, "SE10 9NF", Price: 525_000m, FloorArea: 68m, AreaUnit: FloorAreaUnit.SquareMetres, Bedrooms: 2, EpcRating: "C", MonthlyCouncilTax: 165m),
            new Listing("Croydon terrace", ListingMode.Buy, "CR0 6BE", Price: 410_000m, FloorArea: 74m, AreaUnit: FloorAreaUnit.SquareMetres, Bedrooms: 2),
        ]);

        var response = await client.PostAsJsonAsync("/api/comparison", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("listings").GetArrayLength(), Is.EqualTo(2));
            Assert.That(root.GetProperty("listings")[0].GetProperty("pricePerSquareFoot").GetDecimal(), Is.GreaterThan(0));
            Assert.That(root.GetProperty("listings")[0].GetProperty("completenessPercent").GetInt32(), Is.GreaterThan(0));
            Assert.That(root.GetProperty("highlights").GetArrayLength(), Is.GreaterThan(0));
            Assert.That(root.GetProperty("caveats").GetArrayLength(), Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task Comparison_with_one_listing_returns_problem_details()
    {
        var client = _factory.CreateClient();
        var request = new ComparisonRequest([
            new Listing("Only one", ListingMode.Buy, "SE10 9NF", Price: 525_000m),
        ]);

        var response = await client.PostAsJsonAsync("/api/comparison", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }
}
