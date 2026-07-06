using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// The register cross-check merge logic, with a fake geocoder (offline, deterministic): a resolved
// location enriches the draft with register provenance; an approximate outcode centroid is flagged;
// an unresolved postcode leaves the text-only draft untouched.
[TestFixture]
public class RegisterCrossCheckTests
{
    private static ListingExtractionResult Extracted(string postcode = "S20")
        => new(new Listing("A place", ListingMode.Rent, postcode, MonthlyRent: 900m), [], []);

    private sealed class FakeGeocoder(GeocodeResult? result) : IPostcodeGeocoder
    {
        public Task<GeocodeResult?> GeocodeAsync(string postcode, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    [Test]
    public async Task Sets_the_location_and_marks_it_register_provenance()
    {
        var check = new RegisterCrossCheck(new FakeGeocoder(new GeocodeResult(51.5, -0.14, "Westminster", false)));

        var result = await check.EnrichAsync(Extracted(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Draft.Latitude, Is.EqualTo(51.5));
            Assert.That(result.Draft.Longitude, Is.EqualTo(-0.14));
            var location = result.Fields.Single(f => f.Field == "Location");
            Assert.That(location.Source, Is.EqualTo(FieldProvenance.Register));
            Assert.That(location.Confidence, Is.EqualTo(FieldConfidence.High));
        });
    }

    [Test]
    public async Task An_outcode_centroid_is_approximate_with_a_note()
    {
        var check = new RegisterCrossCheck(new FakeGeocoder(new GeocodeResult(54.0, -1.0, "York", true)));

        var result = await check.EnrichAsync(Extracted("YO32"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fields.Single(f => f.Field == "Location").Confidence, Is.EqualTo(FieldConfidence.Medium));
            Assert.That(result.Notes.Any(n => n.Contains("centroid") && n.Contains("York")), Is.True);
        });
    }

    [Test]
    public async Task An_unresolved_postcode_leaves_the_draft_unchanged()
    {
        var input = Extracted();
        var check = new RegisterCrossCheck(new FakeGeocoder(null));

        var result = await check.EnrichAsync(input, CancellationToken.None);

        Assert.That(result, Is.SameAs(input));
        Assert.That(result.Draft.Latitude, Is.Null);
    }
}
