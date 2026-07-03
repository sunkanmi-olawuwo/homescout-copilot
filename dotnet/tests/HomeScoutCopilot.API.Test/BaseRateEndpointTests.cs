using System.Net.Http.Json;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeScoutCopilot.API.Test;

// Contract test for GET /api/mortgage/base-rate. The real provider is replaced with a
// stub so the test makes no live Bank of England call.
[TestFixture]
public class BaseRateEndpointTests
{
    [Test]
    public async Task Base_rate_endpoint_returns_the_provider_value()
    {
        var stubValue = new BaseRate(3.75m, new DateOnly(2026, 6, 19), "Fallback", "Bank of England", "Context only.");

        using var factory = new WebApplicationFactory<HomeScoutCopilot.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBaseRateProvider>();
                services.AddSingleton<IBaseRateProvider>(new StubBaseRateProvider(stubValue));
            }));

        var response = await factory.CreateClient().GetAsync("/api/mortgage/base-rate");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<BaseRate>();
        Assert.Multiple(() =>
        {
            Assert.That(body, Is.Not.Null);
            Assert.That(body!.RatePercent, Is.EqualTo(3.75m));
            Assert.That(body.EffectiveDate, Is.EqualTo(new DateOnly(2026, 6, 19)));
            Assert.That(body.Source, Is.EqualTo("Bank of England"));
        });
    }
}

internal sealed class StubBaseRateProvider(BaseRate value) : IBaseRateProvider
{
    public Task<BaseRate> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(value);
}
