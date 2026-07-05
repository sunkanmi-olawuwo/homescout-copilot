using System.Globalization;
using System.Text.RegularExpressions;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Deterministic, offline parse of a listing's extracted text into a draft <see cref="Listing"/> plus
/// per-field provenance/confidence. It reads the labelled fields and spec block that portals emit
/// (Rightmove, Zoopla, OpenRent each use a different layout, so this matches labels, not templates).
/// It never guesses: an absent field is simply not populated. This is the text layer of the capture
/// pipeline — the vision and register layers are added in later slices; the user always confirms.
/// </summary>
public interface IListingFactParser
{
    ListingExtractionResult Parse(string text, string? sourceUrl);
}

public sealed partial class ListingFactParser : IListingFactParser
{
    public ListingExtractionResult Parse(string text, string? sourceUrl)
    {
        var t = Collapse(text);
        var fields = new List<FieldExtraction>();

        var mode = DetectMode(t);
        var (label, postcode, address) = DetectLocation(t);

        var draft = new Listing(Label: label, Mode: mode, Postcode: postcode)
        {
            SourceUrl = sourceUrl,
            AddressLine = address,
            Price = mode == ListingMode.Buy ? Money(PriceRegex(), t, fields, "Price", FieldConfidence.Medium) : null,
            PriceQualifier = mode == ListingMode.Buy ? DetectQualifier(t, fields) : null,
            MonthlyRent = mode == ListingMode.Rent ? DetectRent(t, fields) : null,
            Bedrooms = Int(BedroomsRegex(), t, fields, "Bedrooms"),
            Bathrooms = Int(BathroomsRegex(), t, fields, "Bathrooms"),
            Receptions = Int(ReceptionsRegex(), t, fields, "Receptions"),
            PropertyType = DetectPropertyType(t, fields),
            Tenure = DetectTenure(t, fields),
            EpcRating = DetectEpc(t, fields),
            CouncilTaxBand = DetectBand(t, fields),
            Furnishing = mode == ListingMode.Rent ? DetectFurnishing(t, fields) : null,
        };

        (draft, var areaFields) = ApplyFloorArea(draft, t);
        fields.AddRange(areaFields);

        if (postcode.Length > 0)
        {
            fields.Add(new FieldExtraction("Postcode", FieldProvenance.Text, FieldConfidence.High));
        }

        return new ListingExtractionResult(draft, fields, Notes(draft));
    }

    // --- location -----------------------------------------------------------------------------

    private static (string Label, string Postcode, string? Address) DetectLocation(string t)
    {
        // Scope location to the title region (browser-saved pages start with it) so we don't pick up
        // the agent's or footer's postcode/address from the body.
        var title = t.Length > 180 ? t[..180] : t;
        var outward = OutwardCodeRegex().Match(title);
        var postcode = outward.Success ? outward.Value : string.Empty;

        var street = StreetRegex().Match(title);
        var address = street.Success ? street.Groups[1].Value.Trim() : null;

        var label = address is not null && postcode.Length > 0 ? $"{address}, {postcode}"
            : address ?? (postcode.Length > 0 ? postcode : "Untitled listing");
        return (label, postcode, address);
    }

    // --- mode ---------------------------------------------------------------------------------

    private static ListingMode DetectMode(string t) =>
        RentSignalRegex().IsMatch(t) && !ForSaleRegex().IsMatch(t) ? ListingMode.Rent
        : ForSaleRegex().IsMatch(t) ? ListingMode.Buy
        : RentSignalRegex().IsMatch(t) ? ListingMode.Rent
        : ListingMode.Buy;

    // --- typed field helpers ------------------------------------------------------------------

    private static decimal? Money(Regex re, string t, List<FieldExtraction> fields, string field, FieldConfidence conf)
    {
        var m = re.Match(t);
        if (!m.Success || !decimal.TryParse(m.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
        {
            return null;
        }

        fields.Add(new FieldExtraction(field, FieldProvenance.Text, conf));
        return v;
    }

    private static int? Int(Regex re, string t, List<FieldExtraction> fields, string field)
    {
        var m = re.Match(t);
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var v))
        {
            return null;
        }

        fields.Add(new FieldExtraction(field, FieldProvenance.Text, FieldConfidence.Medium));
        return v;
    }

    private static (Listing, IReadOnlyList<FieldExtraction>) ApplyFloorArea(Listing draft, string t)
    {
        var m = FloorAreaRegex().Match(t);
        if (!m.Success || !decimal.TryParse(m.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var area))
        {
            return (draft, []);
        }

        var unit = m.Groups[2].Value.StartsWith('m') ? FloorAreaUnit.SquareMetres : FloorAreaUnit.SquareFeet;
        return (draft with { FloorArea = area, AreaUnit = unit },
            [new FieldExtraction("FloorArea", FieldProvenance.Text, FieldConfidence.High)]);
    }

    // Rent may be written "£925 pcm" or "Rent PCM £925" — try both orders.
    private static decimal? DetectRent(string t, List<FieldExtraction> fields)
        => Money(RentRegex(), t, fields, "MonthlyRent", FieldConfidence.High)
           ?? Money(RentLabelledRegex(), t, fields, "MonthlyRent", FieldConfidence.High);

    private static string? DetectPropertyType(string t, List<FieldExtraction> fields)
    {
        var m = PropertyTypeRegex().Match(t);
        if (!m.Success)
        {
            return null;
        }

        fields.Add(new FieldExtraction("PropertyType", FieldProvenance.Text, FieldConfidence.Medium));
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Value.ToLowerInvariant());
    }

    private static string? DetectEpc(string t, List<FieldExtraction> fields)
    {
        var m = EpcRegex().Match(t);
        if (!m.Success)
        {
            return null;
        }

        fields.Add(new FieldExtraction("EpcRating", FieldProvenance.Text, FieldConfidence.Medium));
        return m.Groups[1].Value.ToUpperInvariant();
    }

    private static CouncilTaxBand? DetectBand(string t, List<FieldExtraction> fields)
    {
        var m = CouncilBandRegex().Match(t);
        if (!m.Success || !Enum.TryParse<CouncilTaxBand>(m.Groups[1].Value.ToUpperInvariant(), out var band))
        {
            return null;
        }

        fields.Add(new FieldExtraction("CouncilTaxBand", FieldProvenance.Text, FieldConfidence.High));
        return band;
    }

    private static PropertyTenure? DetectTenure(string t, List<FieldExtraction> fields)
    {
        PropertyTenure? tenure =
            ShareOfFreeholdRegex().IsMatch(t) ? PropertyTenure.ShareOfFreehold
            : Contains(t, "leasehold") ? PropertyTenure.Leasehold
            : Contains(t, "freehold") ? PropertyTenure.Freehold
            : null;
        if (tenure is not null)
        {
            fields.Add(new FieldExtraction("Tenure", FieldProvenance.Text, FieldConfidence.High));
        }

        return tenure;
    }

    private static FurnishingState? DetectFurnishing(string t, List<FieldExtraction> fields)
    {
        FurnishingState? furnishing =
            Contains(t, "tenant choice") || Contains(t, "furnished or unfurnished") ? FurnishingState.AtTenantChoice
            : Contains(t, "part furnished") || Contains(t, "part-furnished") ? FurnishingState.PartFurnished
            : Contains(t, "unfurnished") ? FurnishingState.Unfurnished
            : Contains(t, "furnished") ? FurnishingState.Furnished
            : null;
        if (furnishing is not null)
        {
            fields.Add(new FieldExtraction("Furnishing", FieldProvenance.Text, FieldConfidence.High));
        }

        return furnishing;
    }

    private static PriceQualifier? DetectQualifier(string t, List<FieldExtraction> fields)
    {
        PriceQualifier? q =
            Contains(t, "guide price") ? PriceQualifier.Guide
            : Contains(t, "offers over") || Contains(t, "offers in excess") ? PriceQualifier.OffersOver
            : Contains(t, "offers in region") || Contains(t, "oiro") ? PriceQualifier.OffersInRegionOf
            : Contains(t, "poa") || Contains(t, "price on application") ? PriceQualifier.Poa
            : null;
        if (q is not null)
        {
            fields.Add(new FieldExtraction("PriceQualifier", FieldProvenance.Text, FieldConfidence.High));
        }

        return q;
    }

    private static IReadOnlyList<string> Notes(Listing d)
    {
        var notes = new List<string>();
        if (d.Postcode.Length is > 0 and <= 4)
        {
            notes.Add("Only an outward postcode was found — add the full postcode for precise area evidence.");
        }

        if (d.FloorArea is null)
        {
            notes.Add("No floor area on the listing — add it (or the EPC) to get price per ft²/m².");
        }

        if (d.CouncilTaxBand is null && d.MonthlyCouncilTax is null)
        {
            notes.Add("No council tax band found — it can be looked up from the postcode.");
        }

        return notes;
    }

    private static bool Contains(string t, string term) => t.Contains(term, StringComparison.OrdinalIgnoreCase);
    private static string Collapse(string t) => WhitespaceRegex().Replace(t, " ");

    // --- patterns -----------------------------------------------------------------------------

    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"to rent|to let|pcm|p\s*/\s*m|per month|rent pcm", RegexOptions.IgnoreCase)] private static partial Regex RentSignalRegex();
    [GeneratedRegex(@"for sale|guide price|offers over|offers in region|freehold|leasehold", RegexOptions.IgnoreCase)] private static partial Regex ForSaleRegex();
    [GeneratedRegex(@"£\s*([\d,]+(?:\.\d{2})?)\s*(?:pcm|p\s*/\s*m|per month)", RegexOptions.IgnoreCase)] private static partial Regex RentRegex();
    [GeneratedRegex(@"(?:rent\s*pcm|pcm|per month)\s*£\s*([\d,]+(?:\.\d{2})?)", RegexOptions.IgnoreCase)] private static partial Regex RentLabelledRegex();
    [GeneratedRegex(@"(?:guide price|offers over|offers in region of|price|£)\D{0,12}£?\s*([\d]{2,3}(?:,\d{3})+)", RegexOptions.IgnoreCase)] private static partial Regex PriceRegex();
    [GeneratedRegex(@"\b(\d{1,2})\s*bed(?:room)?s?\b", RegexOptions.IgnoreCase)] private static partial Regex BedroomsRegex();
    [GeneratedRegex(@"(\d+)\s*bath(?:room)?s?\b", RegexOptions.IgnoreCase)] private static partial Regex BathroomsRegex();
    [GeneratedRegex(@"(\d+)\s*reception", RegexOptions.IgnoreCase)] private static partial Regex ReceptionsRegex();
    [GeneratedRegex(@"([\d,]+(?:\.\d+)?)\s*sq\.?\s*(ft|feet|m|metres|meters)(?![a-z])", RegexOptions.IgnoreCase)] private static partial Regex FloorAreaRegex();
    [GeneratedRegex(@"(?:EPC|Energy)\s*Rating\s*[:\-]?\s*([A-G])(?![a-z])", RegexOptions.IgnoreCase)] private static partial Regex EpcRegex();
    [GeneratedRegex(@"council tax band\s*[:\-]?\s*([A-H])(?![a-z])", RegexOptions.IgnoreCase)] private static partial Regex CouncilBandRegex();
    [GeneratedRegex(@"share of freehold", RegexOptions.IgnoreCase)] private static partial Regex ShareOfFreeholdRegex();
    [GeneratedRegex(@"\b(detached bungalow|semi-detached house|semi-detached|detached house|terraced house|end of terrace|mid-terrace|town house|apartment|maisonette|bungalow|flat|studio)\b", RegexOptions.IgnoreCase)] private static partial Regex PropertyTypeRegex();
    [GeneratedRegex(@"\b([A-Z]{1,2}\d[A-Z\d]?)\b")] private static partial Regex OutwardCodeRegex();
    [GeneratedRegex(@"([A-Z][a-z]+(?: [A-Z][a-z]+){0,2} (?:Way|Road|Street|Lane|Avenue|Close|Drive|Court|Gardens|Place|Terrace|Row|Grove|Crescent|Rise|Walk)\b)")] private static partial Regex StreetRegex();
}
