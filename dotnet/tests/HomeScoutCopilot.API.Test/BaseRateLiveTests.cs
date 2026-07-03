using System.Net.Http.Json;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeScoutCopilot.API.Test;

// Live verification that the Bank of England base-rate fetch actually works through
// the fully-wired app (real HttpClient + User-Agent + parser). Makes a real network
// call, so it is [Category("Integration")] and excluded from the fast/offline gate;
// run it on demand or on a schedule to catch the source blocking us or changing format.
//
//   dotnet test --filter "FullyQualifiedName~BaseRateLiveTests"
//
// A "Fallback" result here means the live path is broken — that is exactly what this
// test is here to surface (the offline suite can't, because it stubs the source).
[TestFixture]
[Category("Integration")] // excluded from the fast PR gate
[Category("External")]    // targeted by the nightly external-checks workflow
public class BaseRateLiveTests
{
    [Test]
    public async Task Base_rate_is_fetched_live_from_the_Bank_of_England()
    {
        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>();

        var body = await factory.CreateClient().GetFromJsonAsync<BaseRate>("/api/mortgage/base-rate");

        Assert.That(body, Is.Not.Null);
        Assert.That(
            body!.Provenance,
            Is.EqualTo("Live"),
            "expected a live Bank of England value but got a fallback — check connectivity, the endpoint/series, or the User-Agent");
        Assert.That(body.RatePercent, Is.GreaterThan(0m).And.LessThan(25m));
        Assert.That(body.EffectiveDate, Is.GreaterThan(new DateOnly(2020, 1, 1)));
    }
}
