using System.Text.Json;
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

    [Test]
    public async Task ComparisonSample_returns_ok_with_title_and_summary()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/comparison/sample");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.That(root.GetProperty("title").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(root.GetProperty("summary").GetString(), Is.Not.Null.And.Not.Empty);
    }
}
