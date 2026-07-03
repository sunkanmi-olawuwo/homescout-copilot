namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>
/// The Bank of England base rate, provided for orientation only. It is <b>not</b> a
/// mortgage product rate and must never be used as the interest-rate input to a cost
/// estimate. <see cref="Provenance"/> is "Live", "Cache", or "Fallback".
/// </summary>
public record BaseRate(
    decimal RatePercent,
    DateOnly EffectiveDate,
    string Provenance,
    string Source,
    string Note);
