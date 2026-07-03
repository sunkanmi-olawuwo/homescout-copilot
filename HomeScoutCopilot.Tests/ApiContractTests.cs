using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeScoutCopilot.Tests;

// Fast, node-free contract tests: boot the API in-memory and assert the public
// response shape. These are the behaviour-lock through the relocation/layering
// phases — they must keep passing unedited while projects move and split.
public class ApiContractTests : IClassFixture<WebApplicationFactory<HomeScoutCopilot.ApiService.ApiMarker>>
{
    private readonly WebApplicationFactory<HomeScoutCopilot.ApiService.ApiMarker> _factory;

    public ApiContractTests(WebApplicationFactory<HomeScoutCopilot.ApiService.ApiMarker> factory) => _factory = factory;

    [Fact]
    public async Task Status_returns_ok_with_expected_shape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("HomeScout Copilot", root.GetProperty("product").GetString());
        Assert.Equal("React", root.GetProperty("frontend").GetString());
        Assert.Equal("API-first", root.GetProperty("architecture").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("agentPlatform").GetString()));
    }

    [Fact]
    public async Task ComparisonSample_returns_ok_with_title_and_summary()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/comparison/sample", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("summary").GetString()));
    }
}
