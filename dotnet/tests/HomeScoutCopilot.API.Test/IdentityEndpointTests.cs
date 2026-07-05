using System.Net.Http.Json;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HomeScoutCopilot.API.Test;

// Offline auth tests for GET /api/me using the TestAuthHandler stub (no live Keycloak). Locks the
// contract: authorized-only, resolves the token subject + profile, and rejects a missing subject.
[TestFixture]
public class IdentityEndpointTests
{
    private static WebApplicationFactory<HomeScoutCopilot.API.ApiMarker> Factory() =>
        new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })));

    [Test]
    public async Task Me_requires_authentication()
    {
        using var factory = Factory();

        // No X-Test-Subject header -> anonymous -> 401.
        var response = await factory.CreateClient().GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_returns_the_token_identity_when_authenticated()
    {
        using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-123");

        var response = await client.GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(me, Is.Not.Null);
            Assert.That(me!.Subject, Is.EqualTo("user-123"));
            Assert.That(me.Email, Is.EqualTo("test.user@homescout.local"));
            Assert.That(me.Name, Is.EqualTo("Test User"));
        });
    }

    [Test]
    public async Task Me_returns_401_when_the_token_has_no_subject()
    {
        using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.OmitSubjectHeader, "true");

        var response = await client.GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Copilot_ask_stays_anonymous_capable_alongside_auth()
    {
        // Auth is additive: with the scheme registered but no credentials, the anonymous copilot
        // endpoint still works (503 here only because no gateway is configured — not a 401).
        using var factory = Factory();

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("hello"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }
}
