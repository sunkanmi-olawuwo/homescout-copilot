using FluentValidation;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Configuration for <see cref="BankOfEnglandBaseRateProvider"/>. The source URL and
/// series code are configurable so the authoritative endpoint can be tuned without a
/// code change; the fallback is the last-known value shown when the source is unreachable.
/// </summary>
public sealed class BaseRateOptions : IValidatedOptions<BaseRateOptions>
{
    public static string SectionName => "BaseRate";

    /// <summary>
    /// Bank of England Interactive Database CSV endpoint. Format args: {0} = date-from,
    /// {1} = date-to (both dd/MMM/yyyy), {2} = series code.
    /// </summary>
    public string EndpointFormat { get; set; } =
        "https://www.bankofengland.co.uk/boeapps/database/_iadb-fromshowcolumns.asp?csv.x=yes&Datefrom={0}&Dateto={1}&SeriesCodes={2}&CSVF=TN&UsingCodes=Y&VPD=Y&VFD=N";

    /// <summary>Official Bank Rate series code.</summary>
    public string SeriesCode { get; set; } = "IUDBEDR";

    public int LookbackDays { get; set; } = 400;

    /// <summary>Cache lifetime for a good value — the rate changes at most a few times a year.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Shorter cache for a fallback value so we retry the source sooner.</summary>
    public TimeSpan FallbackCacheTtl { get; set; } = TimeSpan.FromMinutes(15);

    public decimal FallbackRatePercent { get; set; } = 3.75m;

    public DateOnly FallbackEffectiveDate { get; set; } = new(2026, 6, 19);

    public IValidator<BaseRateOptions> GetValidator() => new Validator();

    private sealed class Validator : AbstractValidator<BaseRateOptions>
    {
        public Validator()
        {
            RuleFor(x => x.EndpointFormat).NotEmpty();
            RuleFor(x => x.SeriesCode).NotEmpty();
            RuleFor(x => x.LookbackDays).GreaterThan(0);
            RuleFor(x => x.FallbackRatePercent).InclusiveBetween(0m, 25m);
        }
    }
}
