using System.Text.Json;
using System.Text.RegularExpressions;

namespace HomeScoutCopilot.API.Service;

/// <summary>A resolved location. <see cref="Approximate"/> is true when only an outward code was
/// available (the district/area centroid, not the property) — a full postcode gives the exact point.</summary>
public record GeocodeResult(double Latitude, double Longitude, string? District, bool Approximate);

/// <summary>Resolves a UK postcode to a location against an authoritative source. Best-effort and
/// non-throwing: an unresolvable postcode or an unreachable service returns null, so it never breaks
/// extraction (the text-only draft stands).</summary>
public interface IPostcodeGeocoder
{
    Task<GeocodeResult?> GeocodeAsync(string postcode, CancellationToken cancellationToken);
}

/// <summary>Geocoder over postcodes.io (open ONS data, no API key). Full postcodes hit
/// <c>/postcodes/{pc}</c> (exact); outward codes hit <c>/outcodes/{oc}</c> (area centroid).</summary>
public sealed partial class PostcodesIoGeocoder(HttpClient httpClient) : IPostcodeGeocoder
{
    public async Task<GeocodeResult?> GeocodeAsync(string postcode, CancellationToken cancellationToken)
    {
        var normalised = postcode.Trim().ToUpperInvariant().Replace(" ", "");
        var (path, approximate) =
            FullPostcodeRegex().IsMatch(normalised) ? ($"postcodes/{Uri.EscapeDataString(normalised)}", false)
            : OutwardCodeRegex().IsMatch(normalised) ? ($"outcodes/{Uri.EscapeDataString(normalised)}", true)
            : (null, false);
        if (path is null)
        {
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return Parse(doc.RootElement, approximate);
        }
        catch (Exception)
        {
            // Network/timeout/parse failure — degrade to text-only extraction, never throw.
            return null;
        }
    }

    private static GeocodeResult? Parse(JsonElement root, bool approximate)
    {
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object
            || result.GetProperty("latitude").ValueKind != JsonValueKind.Number
            || result.GetProperty("longitude").ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return new GeocodeResult(
            result.GetProperty("latitude").GetDouble(),
            result.GetProperty("longitude").GetDouble(),
            District(result),
            approximate);
    }

    // admin_district is a string for a full postcode and an array for an outward code.
    private static string? District(JsonElement result)
    {
        if (!result.TryGetProperty("admin_district", out var d))
        {
            return null;
        }

        return d.ValueKind switch
        {
            JsonValueKind.String => d.GetString(),
            JsonValueKind.Array when d.GetArrayLength() > 0 => d[0].GetString(),
            _ => null,
        };
    }

    [GeneratedRegex(@"^[A-Z]{1,2}\d[A-Z\d]?\d[A-Z]{2}$")] private static partial Regex FullPostcodeRegex();
    [GeneratedRegex(@"^[A-Z]{1,2}\d[A-Z\d]?$")] private static partial Regex OutwardCodeRegex();
}

/// <summary>Geocoder that resolves nothing — the graceful default when postcode lookup is disabled.</summary>
public sealed class NullPostcodeGeocoder : IPostcodeGeocoder
{
    public Task<GeocodeResult?> GeocodeAsync(string postcode, CancellationToken cancellationToken)
        => Task.FromResult<GeocodeResult?>(null);
}
