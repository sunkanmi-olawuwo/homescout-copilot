using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Verifies/enriches an extracted draft against authoritative registers — the "verify, don't trust
/// the listing" layer. Currently resolves the postcode to a location (postcodes.io); the EPC register
/// and council-tax-band → £ derivation slot in behind this same seam next. Best-effort: a register
/// that returns nothing leaves the text-only draft untouched. The user still confirms everything.
/// </summary>
public interface IRegisterCrossCheck
{
    Task<ListingExtractionResult> EnrichAsync(ListingExtractionResult extracted, CancellationToken cancellationToken);
}

public sealed class RegisterCrossCheck(IPostcodeGeocoder geocoder) : IRegisterCrossCheck
{
    public async Task<ListingExtractionResult> EnrichAsync(ListingExtractionResult extracted, CancellationToken cancellationToken)
    {
        var location = await geocoder.GeocodeAsync(extracted.Draft.Postcode, cancellationToken);
        if (location is null)
        {
            return extracted;
        }

        // A resolved location is register-verified; an outcode centroid is only approximate.
        var confidence = location.Approximate ? FieldConfidence.Medium : FieldConfidence.High;
        var fields = new List<FieldExtraction>(extracted.Fields)
        {
            new("Location", FieldProvenance.Register, confidence),
        };

        var notes = new List<string>(extracted.Notes);
        if (location.Approximate)
        {
            var area = location.District is null ? extracted.Draft.Postcode : $"{extracted.Draft.Postcode} ({location.District})";
            notes.Add($"Location is the {area} area centroid — add the full postcode for this property's exact position.");
        }

        var draft = extracted.Draft with { Latitude = location.Latitude, Longitude = location.Longitude };
        return new ListingExtractionResult(draft, fields, notes);
    }
}

/// <summary>Cross-check that changes nothing — the graceful default when register lookups are disabled
/// (and used in offline tests to keep extraction deterministic and network-free).</summary>
public sealed class NullRegisterCrossCheck : IRegisterCrossCheck
{
    public Task<ListingExtractionResult> EnrichAsync(ListingExtractionResult extracted, CancellationToken cancellationToken)
        => Task.FromResult(extracted);
}
