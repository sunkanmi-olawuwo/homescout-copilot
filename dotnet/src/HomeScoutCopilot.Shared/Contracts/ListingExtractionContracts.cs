namespace HomeScoutCopilot.Shared.Contracts;

/// <summary>Where an extracted field came from. <see cref="None"/> means it wasn't found — surfaced as
/// a gap for the user to fill, never guessed.</summary>
public enum FieldProvenance
{
    Text,
    Vision,
    Register,
    None,
}

/// <summary>How sure the pipeline is about an extracted field. Low/conflicting fields are pre-flagged
/// for the user on the confirm screen.</summary>
public enum FieldConfidence
{
    High,
    Medium,
    Low,
}

/// <summary>Provenance + confidence for one populated field of the draft <see cref="Listing"/>, so the
/// confirm UI can show where each fact came from and pre-flag the shaky ones.</summary>
public record FieldExtraction(string Field, FieldProvenance Source, FieldConfidence Confidence);

/// <summary>Result of <c>POST /api/listings/extract</c>: a draft <see cref="Listing"/> (unconfirmed,
/// fields nullable) plus the per-field provenance/confidence sidecar. Nothing here is used or sent to
/// the agent until the user confirms it. Extraction proposes; the user ratifies.</summary>
public record ListingExtractionResult(
    Listing Draft,
    IReadOnlyList<FieldExtraction> Fields,
    IReadOnlyList<string> Notes);
