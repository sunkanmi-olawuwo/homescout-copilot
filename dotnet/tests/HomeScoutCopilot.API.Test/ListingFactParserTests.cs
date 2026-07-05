using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Test;

// Locks the deterministic text parser against the three real-world listing layouts (for-sale spec
// block, and two labelled rent formats). Fixtures are synthetic — they mimic the space-separated text
// the PDF reader produces, with invented values — so no listing content is committed. It must never
// guess: an absent fact stays null and is surfaced as a note.
[TestFixture]
public class ListingFactParserTests
{
    private readonly ListingFactParser _parser = new();

    // A for-sale layout (Rightmove-style spec block: type, beds, baths, size, tenure + labelled lines).
    private const string ForSaleText =
        "3 bedroom detached bungalow for sale in Test Way, Faketon, AB12 " +
        "Detached Bungalow 3 2 1,200 sq ft 111 sq m Freehold Guide Price £450,000 Added on 01/01/2026 " +
        "Council Tax Band: D Energy Performance Certificate";

    // A rent layout with an inline spec line and colon-labelled facts (Zoopla-style).
    private const string RentInlineText =
        "2 bed flat to rent Test Court, Sampleton CD3 2 beds 1 bath 1 reception 700 sq. ft EPC Rating: C " +
        "Available immediately Unfurnished Council tax band B £850 pcm";

    // A rent layout where the £ follows the label and there is no floor area or council tax (OpenRent-style).
    private const string RentLabelledText =
        "2 Bed Terraced House Test Lane, EF4 Deposit £925.00 Rent PCM £925.00 EPC Rating C Furnishing At tenant choice";

    private Listing Draft(string text) => _parser.Parse(text, "https://example.test/listing").Draft;

    [Test]
    public void For_sale_spec_block_is_parsed()
    {
        var d = Draft(ForSaleText);
        Assert.Multiple(() =>
        {
            Assert.That(d.Mode, Is.EqualTo(ListingMode.Buy));
            Assert.That(d.Price, Is.EqualTo(450_000m));
            Assert.That(d.PriceQualifier, Is.EqualTo(PriceQualifier.Guide));
            Assert.That(d.Bedrooms, Is.EqualTo(3));
            Assert.That(d.FloorArea, Is.EqualTo(1_200m));
            Assert.That(d.AreaUnit, Is.EqualTo(FloorAreaUnit.SquareFeet));
            Assert.That(d.Tenure, Is.EqualTo(PropertyTenure.Freehold));
            Assert.That(d.CouncilTaxBand, Is.EqualTo(CouncilTaxBand.D));
            Assert.That(d.PropertyType, Is.EqualTo("Detached Bungalow"));
            Assert.That(d.Postcode, Is.EqualTo("AB12"));
            Assert.That(d.SourceUrl, Is.EqualTo("https://example.test/listing"));
        });
    }

    [Test]
    public void Rent_inline_layout_is_parsed()
    {
        var d = Draft(RentInlineText);
        Assert.Multiple(() =>
        {
            Assert.That(d.Mode, Is.EqualTo(ListingMode.Rent));
            Assert.That(d.MonthlyRent, Is.EqualTo(850m));
            Assert.That(d.Bedrooms, Is.EqualTo(2));
            Assert.That(d.Bathrooms, Is.EqualTo(1));
            Assert.That(d.Receptions, Is.EqualTo(1));
            Assert.That(d.FloorArea, Is.EqualTo(700m));
            Assert.That(d.EpcRating, Is.EqualTo("C"));
            Assert.That(d.CouncilTaxBand, Is.EqualTo(CouncilTaxBand.B));
            Assert.That(d.Furnishing, Is.EqualTo(FurnishingState.Unfurnished));
        });
    }

    [Test]
    public void Rent_with_label_before_amount_and_tenant_choice_furnishing()
    {
        var d = Draft(RentLabelledText);
        Assert.Multiple(() =>
        {
            Assert.That(d.Mode, Is.EqualTo(ListingMode.Rent));
            Assert.That(d.MonthlyRent, Is.EqualTo(925m)); // "Rent PCM £925.00" — £ after the label
            Assert.That(d.EpcRating, Is.EqualTo("C"));
            Assert.That(d.Furnishing, Is.EqualTo(FurnishingState.AtTenantChoice));
            Assert.That(d.FloorArea, Is.Null);       // not on the listing
            Assert.That(d.CouncilTaxBand, Is.Null);  // not on the listing
        });
    }

    [Test]
    public void Labelled_facts_are_high_confidence_and_inferred_ones_are_medium()
    {
        var fields = _parser.Parse(RentInlineText, null).Fields.ToDictionary(f => f.Field, f => f.Confidence);
        Assert.Multiple(() =>
        {
            Assert.That(fields["MonthlyRent"], Is.EqualTo(FieldConfidence.High));
            Assert.That(fields["CouncilTaxBand"], Is.EqualTo(FieldConfidence.High));
            Assert.That(fields["Furnishing"], Is.EqualTo(FieldConfidence.High));
            Assert.That(fields["Bedrooms"], Is.EqualTo(FieldConfidence.Medium));
        });
    }

    [Test]
    public void Every_populated_field_has_a_text_provenance_entry()
    {
        var result = _parser.Parse(ForSaleText, null);
        // Nothing here is guessed — each recorded field is sourced from the text.
        Assert.That(result.Fields.All(f => f.Source == FieldProvenance.Text), Is.True);
        Assert.That(result.Fields.Any(f => f.Field == "CouncilTaxBand"), Is.True);
        Assert.That(result.Fields.Any(f => f.Field == "EpcRating"), Is.False); // EPC absent from this text
    }

    [Test]
    public void Missing_facts_become_notes_not_guesses()
    {
        var result = _parser.Parse(RentLabelledText, null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Notes.Any(n => n.Contains("floor area")), Is.True);
            Assert.That(result.Notes.Any(n => n.Contains("council tax band")), Is.True);
            Assert.That(result.Notes.Any(n => n.Contains("outward postcode")), Is.True);
        });
    }

    [Test]
    public void Sparse_text_populates_nothing_and_never_throws()
    {
        var result = _parser.Parse("This is just some prose with no property facts at all.", null);
        Assert.That(result.Draft.Price, Is.Null);
        Assert.That(result.Draft.MonthlyRent, Is.Null);
        Assert.That(result.Fields, Is.Empty);
    }
}
