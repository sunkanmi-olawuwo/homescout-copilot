using System.Net.Http.Json;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HomeScoutCopilot.API.Test;

// GET /api/config is the public (unauthenticated) OIDC config the SPA reads at startup.
[TestFixture]
public class ConfigEndpointTests
{
    [Test]
    public async Task Config_is_public_and_reports_auth_disabled_without_keycloak()
    {
        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>();

        var response = await factory.CreateClient().GetAsync("/api/config");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var config = await response.Content.ReadFromJsonAsync<AuthConfigResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(config!.ClientId, Is.EqualTo("homescout-web"));
            Assert.That(config.Audience, Is.EqualTo("homescout-api"));
            Assert.That(config.AuthEnabled, Is.False);
            Assert.That(config.Authority, Is.Null);
        });
    }

    [Test]
    public async Task Config_builds_the_authority_from_the_keycloak_service()
    {
        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["services:keycloak:http:0"] = "http://localhost:8080",
                })));

        var response = await factory.CreateClient().GetAsync("/api/config");

        var config = await response.Content.ReadFromJsonAsync<AuthConfigResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(config!.AuthEnabled, Is.True);
            Assert.That(config.Authority, Is.EqualTo("http://localhost:8080/realms/homescout"));
        });
    }
}
