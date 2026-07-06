using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// Live check against the real postcodes.io (open ONS data, no key). Category("External"): excluded
// from the PR gate (a third-party outage must not block merges) and run on the nightly schedule.
// Proves the adapter's shape against the real service — verify, don't assume.
[TestFixture]
[Category("External")]
public class PostcodesIoGeocoderLiveTests
{
    private static PostcodesIoGeocoder Geocoder()
        => new(new HttpClient { BaseAddress = new Uri("https://api.postcodes.io/") });

    [Test]
    public async Task Full_postcode_resolves_to_an_exact_location()
    {
        var result = await Geocoder().GeocodeAsync("SW1A 1AA", CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Latitude, Is.EqualTo(51.5).Within(0.1));
            Assert.That(result.Longitude, Is.EqualTo(-0.14).Within(0.1));
            Assert.That(result.District, Is.EqualTo("Westminster"));
            Assert.That(result.Approximate, Is.False);
        });
    }

    [Test]
    public async Task Outward_code_resolves_to_an_approximate_centroid()
    {
        var result = await Geocoder().GeocodeAsync("YO32", CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Approximate, Is.True);
            Assert.That(result.District, Is.Not.Null);
        });
    }

    [Test]
    public async Task An_invalid_postcode_returns_null()
    {
        var result = await Geocoder().GeocodeAsync("ZZ99 9ZZ", CancellationToken.None);

        Assert.That(result, Is.Null);
    }
}
