using System.Text;
using FluentResults;
using HomeScoutCopilot.Shared.Contracts;

namespace HomeScoutCopilot.API.Service;

/// <summary>One uploaded document for a single property — its filename and content stream.</summary>
public record UploadedDocument(string FileName, Stream Content);

/// <summary>
/// Orchestrates the capture pipeline for one property's document(s): read the text from each, parse
/// the combined text into a draft <see cref="Listing"/> with per-field provenance/confidence, and
/// hand it back for the user to confirm. This slice is text-only and deterministic; the vision and
/// register-cross-check layers slot in behind the same seam later. Nothing is used until the user
/// confirms. Expected failures are FluentResults, not exceptions.
/// </summary>
public interface IListingExtractor
{
    Task<Result<ListingExtractionResult>> ExtractAsync(
        IReadOnlyList<UploadedDocument> documents,
        string? sourceUrl,
        CancellationToken cancellationToken);
}

public sealed class ListingExtractor(
    ITextDocumentReader reader,
    IListingFactParser parser,
    IRegisterCrossCheck registers) : IListingExtractor
{
    private const int MaxDocuments = 4;

    public async Task<Result<ListingExtractionResult>> ExtractAsync(
        IReadOnlyList<UploadedDocument> documents,
        string? sourceUrl,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return Result.Fail("Upload at least one PDF to extract a listing.");
        }

        if (documents.Count > MaxDocuments)
        {
            return Result.Fail($"Upload at most {MaxDocuments} documents for one property.");
        }

        var combined = new StringBuilder();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text;
            try
            {
                text = reader.ExtractText(document.Content);
            }
            catch (Exception)
            {
                return Result.Fail($"Couldn't read '{document.FileName}' — is it a valid PDF?");
            }

            combined.AppendLine(text);
        }

        if (string.IsNullOrWhiteSpace(combined.ToString()))
        {
            return Result.Fail("No text could be read — the document may be scanned or image-only "
                + "(image extraction is a later slice). Enter the facts manually for now.");
        }

        var parsed = parser.Parse(combined.ToString(), sourceUrl);
        var enriched = await registers.EnrichAsync(parsed, cancellationToken);
        return Result.Ok(enriched);
    }
}
