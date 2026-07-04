using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.API.Test;

// Offline endpoint tests for POST /api/copilot/ask. No Azure: the agent gateway is
// replaced with the fake via ConfigureTestServices, and the "not configured" path is
// the default (no Foundry endpoint set in the test host).
[TestFixture]
public class CopilotEndpointTests
{
    // Enums travel as strings on the wire (FigureKind -> "estimate"); read them the same way.
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Test]
    public async Task Ask_returns_the_gateway_answer_when_configured()
    {
        var canned = new CopilotAnswer(
            "Your estimated monthly repayment is about £1,500.75.",
            [new CopilotToolCall("estimate_mortgage", "£300k, 10% deposit, 4.5%, 25y")],
            [new EvidenceItem("Monthly mortgage payment", "£1,500.75", FigureKind.Estimate, "/api/mortgage/estimate", "Live")],
            ["Rate constant for the term."],
            ["This is an estimate, not mortgage advice."]);

        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<IHomeScoutAgentGateway>(_ => new FakeHomeScoutAgentGateway(_ => canned))));

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("what would the monthly cost be?"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<CopilotAnswer>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(body, Is.Not.Null);
            Assert.That(body!.Text, Does.Contain("1,500.75"));
            Assert.That(body.ToolCalls.Any(t => t.Name == "estimate_mortgage"), Is.True);
            // The structured evidence trail flows through the endpoint (the seam Codex renders).
            Assert.That(body.Evidence, Has.Count.EqualTo(1));
            Assert.That(body.Evidence[0].Kind, Is.EqualTo(FigureKind.Estimate));
            Assert.That(body.Evidence[0].Provenance, Is.EqualTo("Live"));
            Assert.That(body.Evidence[0].Value, Does.Contain("1,500.75"));
        });
    }

    [Test]
    public async Task Ask_returns_503_when_the_copilot_is_not_configured()
    {
        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>();

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("hello"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Ask_returns_400_for_an_empty_message()
    {
        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<IHomeScoutAgentGateway>(_ => new FakeHomeScoutAgentGateway())));

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/copilot/ask", new CopilotRequest("   "));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
