using System.Net.Http.Json;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HomeScoutCopilot.API.Test;

// Locks the owner-threading through /api/copilot/ask: an authenticated request resolves to the
// internal user id and hands it to the gateway (which stamps the durable session — the anon→auth
// hand-off), while an anonymous request passes no owner. Regression guard for the wiring the live
// hand-off check exercises end-to-end.
[TestFixture]
public class AskOwnerThreadingTests
{
    private static readonly Guid FixedUserId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    // An enabled directory that resolves any subject to one fixed internal id (no database).
    private sealed class FakeUserDirectory : IUserDirectory
    {
        public bool IsEnabled => true;

        public Task<UserRecord?> RecordAsync(
            string provider, string subject, string? email, string? name, CancellationToken cancellationToken = default)
            => Task.FromResult<UserRecord?>(new UserRecord(FixedUserId, provider, subject, email, name));
    }

    // Captures the owner the endpoint passes to the gateway.
    private sealed class RecordingAskGateway : IHomeScoutAgentGateway
    {
        public Guid? LastUserId { get; private set; }
        public bool Called { get; private set; }

        public Task<CopilotAnswer> AskAsync(
            CopilotRequest request, string? sessionId = null, Guid? userId = null, CancellationToken cancellationToken = default)
        {
            Called = true;
            LastUserId = userId;
            return Task.FromResult(new CopilotAnswer("ok", [], [], [], []));
        }
    }

    private static (WebApplicationFactory<HomeScoutCopilot.API.ApiMarker> Factory, RecordingAskGateway Gateway) Build()
    {
        var gateway = new RecordingAskGateway();
        var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                // Enabled directory so the resolver returns a non-null internal id for authenticated calls.
                services.AddSingleton<IUserDirectory, FakeUserDirectory>();
                services.AddScoped<IHomeScoutAgentGateway>(_ => gateway);
            }));
        return (factory, gateway);
    }

    [Test]
    public async Task Authenticated_ask_threads_the_internal_owner_to_the_gateway()
    {
        var (factory, gateway) = Build();
        using var _ = factory;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-1");

        var response = await client.PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("hello"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(gateway.Called, Is.True);
            Assert.That(gateway.LastUserId, Is.EqualTo(FixedUserId));
        });
    }

    [Test]
    public async Task Anonymous_ask_passes_no_owner()
    {
        var (factory, gateway) = Build();
        using var _ = factory;

        var response = await factory.CreateClient().PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("hello"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(gateway.Called, Is.True);
            Assert.That(gateway.LastUserId, Is.Null);
        });
    }
}
