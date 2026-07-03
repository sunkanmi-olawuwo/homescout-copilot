using System.Text.Json;
using HomeScoutCopilot.Shared.Application.Contracts;

namespace HomeScoutCopilot.Shared.Application.Test;

// Locks the wire contract: DTOs must serialize to the camelCase property names the
// React frontend and the typed API client depend on.
public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

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
    public void ComparisonSample_round_trips()
    {
        var original = new ComparisonSample("Greenwich vs Croydon", "A summary.");

        var round = JsonSerializer.Deserialize<ComparisonSample>(
            JsonSerializer.Serialize(original, WebOptions), WebOptions);

        Assert.That(round, Is.EqualTo(original));
    }
}
