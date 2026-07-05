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

public sealed class ListingExtractor(ITextDocumentReader reader, IListingFactParser parser) : IListingExtractor
{
    private const int MaxDocuments = 4;

    public Task<Result<ListingExtractionResult>> ExtractAsync(
        IReadOnlyList<UploadedDocument> documents,
        string? sourceUrl,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return Fail("Upload at least one PDF to extract a listing.");
        }

        if (documents.Count > MaxDocuments)
        {
            return Fail($"Upload at most {MaxDocuments} documents for one property.");
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
                return Fail($"Couldn't read '{document.FileName}' — is it a valid PDF?");
            }

            combined.AppendLine(text);
        }

        if (string.IsNullOrWhiteSpace(combined.ToString()))
        {
            return Fail("No text could be read — the document may be scanned or image-only "
                + "(image extraction is a later slice). Enter the facts manually for now.");
        }

        var result = parser.Parse(combined.ToString(), sourceUrl);
        return Task.FromResult(Result.Ok(result));
    }

    private static Task<Result<ListingExtractionResult>> Fail(string message)
        => Task.FromResult(Result.Fail<ListingExtractionResult>(message));
}
