using System.Net.Http.Json;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HomeScoutCopilot.API.Test;

// Offline tests for the history endpoints using the TestAuthHandler stub. The owner-scoping (never
// returns another user's rows) is proven directly against Postgres in PostgresSessionStoreTests;
// here we lock the auth + no-database contract. The test host has no database, so history is empty.
[TestFixture]
public class HistoryEndpointTests
{
    private static WebApplicationFactory<HomeScoutCopilot.API.ApiMarker> Factory() =>
        new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })));

    [Test]
    public async Task History_requires_authentication()
    {
        using var factory = Factory();

        var response = await factory.CreateClient().GetAsync("/api/copilot/history");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task History_is_empty_without_a_database()
    {
        using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-1");

        var response = await client.GetAsync("/api/copilot/history");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<ConversationHistoryResponse>();
        Assert.That(body!.Conversations, Is.Empty);
    }

    [Test]
    public async Task Single_conversation_requires_authentication()
    {
        using var factory = Factory();

        var response = await factory.CreateClient().GetAsync("/api/copilot/history/some-session");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Single_conversation_is_404_without_a_database()
    {
        using var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-1");

        var response = await client.GetAsync("/api/copilot/history/some-session");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
