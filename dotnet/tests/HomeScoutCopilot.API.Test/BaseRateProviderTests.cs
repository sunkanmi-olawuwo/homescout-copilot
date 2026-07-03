using HomeScoutCopilot.API.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Test;

[TestFixture]
public class BaseRateProviderTests
{
    private const string SampleCsv =
        "\"Date\",\"IUDBEDR\"\n\"01 Jan 2024\",5.25\n\"07 Feb 2025\",4.5\n\"19 Jun 2026\",3.75\n";

    private static BankOfEnglandBaseRateProvider Provider(StubHandler handler, BaseRateOptions? options = null) =>
        new(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options ?? new BaseRateOptions()),
            NullLogger<BankOfEnglandBaseRateProvider>.Instance);

    [Test]
    public void TryParseLatest_returns_the_most_recent_row()
    {
        var ok = BankOfEnglandBaseRateProvider.TryParseLatest(SampleCsv, out var rate, out var date);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(rate, Is.EqualTo(3.75m));
            Assert.That(date, Is.EqualTo(new DateOnly(2026, 6, 19)));
        });
    }

    [Test]
    public void TryParseLatest_returns_false_for_unparseable_content()
    {
        var ok = BankOfEnglandBaseRateProvider.TryParseLatest("not,a,rate\n<html>", out _, out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public async Task GetCurrentAsync_returns_live_value_then_serves_from_cache()
    {
        var handler = new StubHandler(_ => Responses.Ok(SampleCsv));
        var provider = Provider(handler);

        var first = await provider.GetCurrentAsync();
        var second = await provider.GetCurrentAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.RatePercent, Is.EqualTo(3.75m));
            Assert.That(first.EffectiveDate, Is.EqualTo(new DateOnly(2026, 6, 19)));
            Assert.That(first.Provenance, Is.EqualTo("Live"));
            Assert.That(first.Source, Is.EqualTo("Bank of England"));
            Assert.That(second.Provenance, Is.EqualTo("Cache"));
            Assert.That(handler.Calls, Is.EqualTo(1), "second call should be served from cache");
        });
    }

    [Test]
    public async Task GetCurrentAsync_falls_back_when_source_throws()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("network down"));
        var options = new BaseRateOptions { FallbackRatePercent = 3.75m, FallbackEffectiveDate = new DateOnly(2026, 6, 19) };
        var provider = Provider(handler, options);

        var result = await provider.GetCurrentAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Provenance, Is.EqualTo("Fallback"));
            Assert.That(result.RatePercent, Is.EqualTo(3.75m));
            Assert.That(result.EffectiveDate, Is.EqualTo(new DateOnly(2026, 6, 19)));
        });
    }

    [Test]
    public async Task GetCurrentAsync_falls_back_on_non_success_status()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var provider = Provider(handler);

        var result = await provider.GetCurrentAsync();

        Assert.That(result.Provenance, Is.EqualTo("Fallback"));
    }
}

internal static class Responses
{
    public static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };
}

internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(responder(request));
    }
}
