using System.Globalization;
using HomeScoutCopilot.Shared.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Fetches the official Bank of England base rate from the Interactive Database CSV
/// series, caches it (~1 day), and falls back to the last-known configured value if the
/// source is unreachable. Never throws — the base rate is context-only.
/// </summary>
public sealed class BankOfEnglandBaseRateProvider : IBaseRateProvider
{
    private const string CacheKey = "boe-base-rate";
    private const string SourceName = "Bank of England";
    private const string ContextNote =
        "Context only — the Bank of England base rate is not a mortgage product rate.";

    private static readonly string[] DateFormats =
        ["d MMM yyyy", "dd MMM yyyy", "d/MMM/yyyy", "dd/MMM/yyyy", "yyyy-MM-dd"];

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly BaseRateOptions _options;
    private readonly ILogger<BankOfEnglandBaseRateProvider> _logger;

    public BankOfEnglandBaseRateProvider(
        HttpClient http,
        IMemoryCache cache,
        IOptions<BaseRateOptions> options,
        ILogger<BankOfEnglandBaseRateProvider> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BaseRate> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out BaseRate? cached) && cached is not null)
        {
            return cached with { Provenance = "Cache" };
        }

        var live = await TryFetchAsync(cancellationToken);
        if (live is not null)
        {
            _cache.Set(CacheKey, live, _options.CacheTtl);
            return live;
        }

        // Never throw into callers: the base rate is context-only.
        var fallback = new BaseRate(
            _options.FallbackRatePercent,
            _options.FallbackEffectiveDate,
            "Fallback",
            SourceName,
            ContextNote);
        _cache.Set(CacheKey, fallback, _options.FallbackCacheTtl);
        return fallback;
    }

    private async Task<BaseRate?> TryFetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(BuildUrl(), cancellationToken);
            response.EnsureSuccessStatusCode();
            var csv = await response.Content.ReadAsStringAsync(cancellationToken);

            if (TryParseLatest(csv, out var rate, out var date))
            {
                return new BaseRate(rate, date, "Live", SourceName, ContextNote);
            }

            _logger.LogWarning("Bank of England base-rate response could not be parsed.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Bank of England base-rate fetch failed; using fallback.");
        }

        return null;
    }

    private string BuildUrl()
    {
        var to = DateTime.UtcNow.Date;
        var from = to.AddDays(-_options.LookbackDays);
        static string Fmt(DateTime d) => d.ToString("dd/MMM/yyyy", CultureInfo.InvariantCulture);

        return string.Format(
            CultureInfo.InvariantCulture,
            _options.EndpointFormat,
            Fmt(from),
            Fmt(to),
            _options.SeriesCode);
    }

    /// <summary>Parses the BoE CSV and returns the most recent (date, rate) row.</summary>
    internal static bool TryParseLatest(string csv, out decimal ratePercent, out DateOnly effectiveDate)
    {
        ratePercent = 0m;
        effectiveDate = default;
        DateOnly? best = null;

        foreach (var raw in csv.Split('\n'))
        {
            var cols = raw.Split(',');
            if (cols.Length < 2)
            {
                continue;
            }

            var left = cols[0].Trim().Trim('"');
            var right = cols[^1].Trim().Trim('"');

            if (!TryParseDate(left, out var date) ||
                !decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            {
                continue;
            }

            if (best is null || date > best)
            {
                best = date;
                ratePercent = rate;
            }
        }

        if (best is null)
        {
            return false;
        }

        effectiveDate = best.Value;
        return true;
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
