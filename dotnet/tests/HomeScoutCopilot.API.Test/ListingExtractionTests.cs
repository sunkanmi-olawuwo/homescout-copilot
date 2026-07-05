using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Regression eval for the REAL extraction pipeline (PdfPig word-level reader + the deterministic
// parser) against real listing PDFs saved from the portals — the thing synthetic fixtures can't cover
// (PdfPig token-spacing quirks, per-site layouts). The corpus lives in TestData/listings (see its
// NOTICE.md); expected facts are the committed ground truth. Add a PDF + a row to grow the corpus.
[TestFixture]
[Category("Extraction")]
public class ListingExtractionTests
{
    // Ground truth. Fields expected absent from the listing text (e.g. an EPC rendered as a graphic)
    // are asserted null — proof the pipeline surfaces gaps rather than guessing.
    public sealed record Expected(
        string File,
        ListingMode Mode,
        string Postcode,
        decimal? Price,
        decimal? Rent,
        int? Beds,
        decimal? Area,
        FloorAreaUnit? Unit,
        string? Epc,
        CouncilTaxBand? Band,
        string? TypeContains)
    {
        public override string ToString() => File;
    }

    // Six real listings across the three sites, both modes, plus edge cases. Ground truth is the
    // TRUE facts of each listing; absent facts (a graphic EPC, "Ask agent" size, an HMO room with no
    // bed count) are asserted null — proof the pipeline surfaces gaps rather than guessing.
    private static readonly Expected[] Corpus =
    [
        // Rightmove — for sale (bungalow). EPC is a graphic on the page → not in the text → null.
        new("rightmove-buy-bungalow.pdf", ListingMode.Buy, "YO32", 500_000m, null, 3, 1443m,
            FloorAreaUnit.SquareFeet, null, CouncilTaxBand.E, "Bungalow"),
        // Rightmove — to rent (semi, listed via OpenRent). Size + council tax are "Ask agent" → null;
        // the headline rent is taken over the conflicting figures in the description.
        new("rightmove-sample1.pdf", ListingMode.Rent, "LS16", null, 1_500m, 3, null,
            null, "C", null, "Semi-Detached"),
        // Zoopla — to rent (flat). EPC + council tax band are in the text.
        new("zoopla-rent-flat.pdf", ListingMode.Rent, "S20", null, 750m, 2, 635m,
            FloorAreaUnit.SquareFeet, "C", CouncilTaxBand.B, "Flat"),
        // Zoopla — for sale (flat). No size/EPC/tenure on the page; council tax band present.
        new("zoopla-buy-sample1.pdf", ListingMode.Buy, "LS7", 225_000m, null, 2, null,
            null, null, CouncilTaxBand.B, "Flat"),
        // OpenRent — to rent (terrace). EPC in the text; no size or council tax band.
        new("openrent-rent-sample1.pdf", ListingMode.Rent, "S2", null, 925m, 2, null,
            null, "C", null, "Terraced House"),
        // OpenRent — a room in a shared house (HMO edge case): no bed count, no standard property
        // type, "EPC Not Required", council tax included in rent → all correctly absent, never guessed.
        new("openrent-rent-sample2.pdf", ListingMode.Rent, "S13", null, 410m, null, null,
            null, null, null, null),
    ];

    [TestCaseSource(nameof(Corpus))]
    public void Real_listing_extracts_the_expected_facts(Expected e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "listings", e.File);
        Assert.That(File.Exists(path), Is.True, $"Fixture not found (check csproj CopyToOutputDirectory): {path}");

        using var stream = File.OpenRead(path);
        var extractor = new ListingExtractor(new PdfDocumentReader(), new ListingFactParser());
        var result = extractor.ExtractAsync([new UploadedDocument(e.File, stream)], null, CancellationToken.None)
            .GetAwaiter().GetResult();

        Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors.Select(x => x.Message)));
        var d = result.Value.Draft;
        Assert.Multiple(() =>
        {
            Assert.That(d.Mode, Is.EqualTo(e.Mode));
            Assert.That(d.Postcode, Is.EqualTo(e.Postcode));
            Assert.That(d.Price, Is.EqualTo(e.Price));
            Assert.That(d.MonthlyRent, Is.EqualTo(e.Rent));
            Assert.That(d.Bedrooms, Is.EqualTo(e.Beds));
            Assert.That(d.FloorArea, Is.EqualTo(e.Area));
            Assert.That(d.AreaUnit, Is.EqualTo(e.Unit));
            Assert.That(d.EpcRating, Is.EqualTo(e.Epc));
            Assert.That(d.CouncilTaxBand, Is.EqualTo(e.Band));
            Assert.That(d.PropertyType, e.TypeContains is null ? Is.Null : Does.Contain(e.TypeContains));
        });
    }
}
