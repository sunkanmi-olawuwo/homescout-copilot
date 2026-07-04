using System.Text.Json;

namespace HomeScoutCopilot.API.Test;

// Offline guard for the committed Keycloak realm export the AppHost imports (WithRealmImport). A
// malformed realm or a renamed client/audience would silently break token validation, so lock the
// shape: realm name, the API (bearer-only) + web (public + PKCE) clients, and the audience mapper.
[TestFixture]
public class KeycloakRealmTests
{
    private static JsonElement Realm()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "dotnet", "src", "HomeScoutCopilot.AppHost", "keycloak", "homescout-realm.json");
        Assert.That(File.Exists(path), Is.True, $"realm export missing at {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    private static JsonElement Client(JsonElement realm, string clientId) =>
        realm.GetProperty("clients").EnumerateArray()
            .Single(c => c.GetProperty("clientId").GetString() == clientId);

    [Test]
    public void Realm_is_named_homescout_and_enabled()
    {
        var realm = Realm();
        Assert.Multiple(() =>
        {
            Assert.That(realm.GetProperty("realm").GetString(), Is.EqualTo("homescout"));
            Assert.That(realm.GetProperty("enabled").GetBoolean(), Is.True);
        });
    }

    [Test]
    public void Api_client_is_bearer_only_with_the_audience_mapper()
    {
        var api = Client(Realm(), "homescout-api");
        Assert.Multiple(() =>
        {
            Assert.That(api.GetProperty("bearerOnly").GetBoolean(), Is.True);
            Assert.That(api.GetProperty("publicClient").GetBoolean(), Is.False);
            // The audience mapper stamps "homescout-api" into access tokens so the API can require it.
            var audience = api.GetProperty("protocolMappers").EnumerateArray()
                .Single(m => m.GetProperty("protocolMapper").GetString() == "oidc-audience-mapper");
            Assert.That(
                audience.GetProperty("config").GetProperty("included.client.audience").GetString(),
                Is.EqualTo("homescout-api"));
        });
    }

    [Test]
    public void Web_client_is_public_with_pkce()
    {
        var web = Client(Realm(), "homescout-web");
        Assert.Multiple(() =>
        {
            Assert.That(web.GetProperty("publicClient").GetBoolean(), Is.True);
            Assert.That(web.GetProperty("standardFlowEnabled").GetBoolean(), Is.True);
            Assert.That(
                web.GetProperty("attributes").GetProperty("pkce.code.challenge.method").GetString(),
                Is.EqualTo("S256"));
        });
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "dotnet", "HomeScoutCopilot.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repo root (HomeScoutCopilot.slnx).");
    }
}
