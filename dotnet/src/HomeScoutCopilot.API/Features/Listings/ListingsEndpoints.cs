using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Listings;

public sealed class ListingsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("/api/listings/extract", async (HttpRequest request, IMediator mediator, CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.Problem(
                        title: "Upload a PDF",
                        detail: "Send the listing document(s) as multipart/form-data.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var form = await request.ReadFormAsync(ct);
                var documents = form.Files
                    .Select(file => new UploadedDocument(file.FileName, file.OpenReadStream()))
                    .ToList();
                var sourceUrl = form["sourceUrl"].FirstOrDefault();

                return await mediator.Send(new ExtractListingCommand(documents, sourceUrl), ct);
            })
            .WithName("ExtractListing")
            .WithTags("Listings")
            .WithSummary("Extract a draft listing from one property's uploaded PDF(s) for the user to confirm")
            .Produces<ListingExtractionResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();
}

public sealed record ExtractListingCommand(IReadOnlyList<UploadedDocument> Documents, string? SourceUrl)
    : IRequest<IResult>;

public sealed class ExtractListingHandler(IListingExtractor extractor)
    : IRequestHandler<ExtractListingCommand, IResult>
{
    public async Task<IResult> Handle(ExtractListingCommand command, CancellationToken cancellationToken)
    {
        var result = await extractor.ExtractAsync(command.Documents, command.SourceUrl, cancellationToken);
        return result.ToHttpResult();
    }
}
