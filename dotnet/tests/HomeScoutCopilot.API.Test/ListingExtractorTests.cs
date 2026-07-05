using HomeScoutCopilot.API.Service;

namespace HomeScoutCopilot.API.Test;

// Locks the extractor orchestration: document-count validation, PDF-read failure handling, the
// empty/scanned-document case, and the happy path. Uses a fake reader so the PDF library isn't
// exercised here (that is the [Category("External")] concern of a later slice).
[TestFixture]
public class ListingExtractorTests
{
    private static UploadedDocument Doc(string name = "listing.pdf") => new(name, new MemoryStream([1, 2, 3]));

    private static ListingExtractor WithReader(Func<Stream, string> read)
        => new(new FakeReader(read), new ListingFactParser());

    private sealed class FakeReader(Func<Stream, string> read) : ITextDocumentReader
    {
        public string ExtractText(Stream pdf) => read(pdf);
    }

    private static Task<FluentResults.Result<Shared.Contracts.ListingExtractionResult>> Extract(
        ListingExtractor extractor, params UploadedDocument[] docs)
        => extractor.ExtractAsync(docs, null, CancellationToken.None);

    [Test]
    public async Task No_documents_fails()
    {
        var result = await Extract(WithReader(_ => "text"));
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("at least one"));
    }

    [Test]
    public async Task More_than_four_documents_fails()
    {
        var docs = Enumerable.Range(0, 5).Select(_ => Doc()).ToArray();
        var result = await Extract(WithReader(_ => "text"), docs);
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("at most 4"));
    }

    [Test]
    public async Task An_unreadable_pdf_fails_gracefully()
    {
        var result = await Extract(WithReader(_ => throw new InvalidOperationException("bad")), Doc("broken.pdf"));
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("broken.pdf"));
    }

    [Test]
    public async Task An_image_only_document_yielding_no_text_fails_with_guidance()
    {
        var result = await Extract(WithReader(_ => "   "), Doc());
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors[0].Message, Does.Contain("scanned or image-only"));
    }

    [Test]
    public async Task Combines_multiple_documents_and_returns_a_draft()
    {
        // A brochure with the price/beds + a separate EPC document, merged into one draft.
        var texts = new Queue<string>(["2 bed flat to rent Test Court, AB1 £900 pcm", "EPC Rating C"]);
        var extractor = WithReader(_ => texts.Dequeue());

        var result = await Extract(extractor, Doc("brochure.pdf"), Doc("epc.pdf"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Draft.MonthlyRent, Is.EqualTo(900m));
            Assert.That(result.Value.Draft.EpcRating, Is.EqualTo("C"));
        });
    }
}
