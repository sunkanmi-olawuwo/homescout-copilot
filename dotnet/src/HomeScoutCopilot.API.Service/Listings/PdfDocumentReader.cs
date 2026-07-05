using System.Text;
using UglyToad.PdfPig;

namespace HomeScoutCopilot.API.Service;

/// <summary>Pulls the raw text out of a PDF. The text layer of the capture pipeline; a scanned or
/// image-only PDF yields little here and is the job of the (later) vision layer.</summary>
public interface ITextDocumentReader
{
    string ExtractText(Stream pdf);
}

public sealed class PdfDocumentReader : ITextDocumentReader
{
    public string ExtractText(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            // page.Text concatenates runs without spaces ("Bungalow321,443 sq ft"); the word-level
            // API reconstructs token spacing, which the labelled-field parser depends on.
            builder.AppendLine(string.Join(' ', page.GetWords().Select(word => word.Text)));
        }

        return builder.ToString();
    }
}
