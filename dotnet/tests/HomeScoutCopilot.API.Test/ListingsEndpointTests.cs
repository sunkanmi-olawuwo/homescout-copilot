using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HomeScoutCopilot.API.Service;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HomeScoutCopilot.API.Test;

// Contract test for POST /api/listings/extract: the multipart upload path, wired to a fake text
// reader so no real PDF is exercised (the PdfPig adapter is validated separately). Confirms the draft
// shape reaches the client and that a non-multipart request is a clean 400.
[TestFixture]
public class ListingsEndpointTests
{
    private WebApplicationFactory<ApiMarker> _factory = null!;

    [OneTimeSetUp]
    public void SetUp() => _factory = new WebApplicationFactory<ApiMarker>().WithWebHostBuilder(builder =>
        // ConfigureTestServices runs after the app's own registration, so these stubs win the resolve.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ITextDocumentReader>(new StubReader(
                "2 bed flat to rent Test Court, AB1 £900 pcm 2 beds 1 bath EPC Rating: C Unfurnished"));
            // Keep the endpoint test network-free — no real postcodes.io call.
            services.AddSingleton<IRegisterCrossCheck, NullRegisterCrossCheck>();
        }));

    [OneTimeTearDown]
    public void TearDown() => _factory.Dispose();

    private sealed class StubReader(string text) : ITextDocumentReader
    {
        public string ExtractText(Stream pdf) => text;
    }

    [Test]
    public async Task Extract_returns_a_draft_listing_from_an_uploaded_pdf()
    {
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "files", "listing.pdf");
        content.Add(new StringContent("https://example.test/listing"), "sourceUrl");

        var response = await client.PostAsync("/api/listings/extract", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var draft = doc.RootElement.GetProperty("draft");
        var mode = draft.GetProperty("mode").GetString();
        var rent = draft.GetProperty("monthlyRent").GetDecimal();
        var epc = draft.GetProperty("epcRating").GetString();
        var source = draft.GetProperty("sourceUrl").GetString();
        var fieldCount = doc.RootElement.GetProperty("fields").GetArrayLength();
        Assert.Multiple(() =>
        {
            Assert.That(mode, Is.EqualTo("Rent"));
            Assert.That(rent, Is.EqualTo(900m));
            Assert.That(epc, Is.EqualTo("C"));
            Assert.That(source, Is.EqualTo("https://example.test/listing"));
            Assert.That(fieldCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task A_non_multipart_request_is_a_clean_400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/listings/extract",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }
}
