using System.Text.Json;
using HomeScoutCopilot.API.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HomeScoutCopilot.API.Test;

// POST /api/copilot/session/resume/{sessionId} re-opens an owned conversation by pointing the
// hs_session cookie at it. Owner-checked: 404 (not 403) for sessions the caller doesn't own.
[TestFixture]
public class ResumeEndpointTests
{
    private static async Task<RecordingSessionStore> StoreWithOwnedSession(string sessionId)
    {
        var store = new RecordingSessionStore();
        using var doc = JsonDocument.Parse("{}");
        await store.SaveAsync(sessionId, doc.RootElement, FakeUserDirectory.FixedUserId);
        return store;
    }

    private static WebApplicationFactory<HomeScoutCopilot.API.ApiMarker> Factory(RecordingSessionStore store) =>
        new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.AddSingleton<IUserDirectory, FakeUserDirectory>();
                services.AddSingleton<ISessionStore>(store);
            }));

    [Test]
    public async Task Resume_requires_authentication()
    {
        using var factory = Factory(await StoreWithOwnedSession("owned"));

        var response = await factory.CreateClient().PostAsync("/api/copilot/session/resume/owned", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Resume_sets_the_session_cookie_for_an_owned_conversation()
    {
        using var factory = Factory(await StoreWithOwnedSession("owned"));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-1");

        var response = await client.PostAsync("/api/copilot/session/resume/owned", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(
            response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.Contains("hs_session=owned")),
            Is.True,
            "resume should point the hs_session cookie at the owned conversation");
    }

    [Test]
    public async Task Resume_is_404_for_a_conversation_the_caller_does_not_own()
    {
        // The store only knows an "owned" session; asking to resume someone else's id is not found.
        using var factory = Factory(await StoreWithOwnedSession("owned"));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "user-1");

        var response = await client.PostAsync("/api/copilot/session/resume/not-mine", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
