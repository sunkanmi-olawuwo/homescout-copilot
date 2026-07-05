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
        string TypeContains)
    {
        public override string ToString() => File;
    }

    private static readonly Expected[] Corpus =
    [
        // Rightmove — for sale. EPC is a graphic on the page → not in the text → expected null.
        new("rightmove-buy-bungalow.pdf", ListingMode.Buy, "YO32", 500_000m, null, 3, 1443m,
            FloorAreaUnit.SquareFeet, null, CouncilTaxBand.E, "Bungalow"),
        // Zoopla — to rent. EPC and council tax band are in the text.
        new("zoopla-rent-flat.pdf", ListingMode.Rent, "S20", null, 750m, 2, 635m,
            FloorAreaUnit.SquareFeet, "C", CouncilTaxBand.B, "Flat"),
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
            Assert.That(d.PropertyType, Does.Contain(e.TypeContains));
        });
    }
}
