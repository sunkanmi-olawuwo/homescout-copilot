using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Comparison;

public sealed class ComparisonEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("/api/comparison", (IMediator mediator, ComparisonRequest request)
                => mediator.Send(new CompareListingsCommand(request)))
            .WithName("CompareListings")
            .WithTags("Comparison")
            .WithSummary("Compare 2–4 listings side by side from their facts (price per ft², indicative monthly cost, completeness)")
            .Produces<ComparisonResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed record CompareListingsCommand(ComparisonRequest Request) : IRequest<IResult>;

public sealed class CompareListingsHandler(IListingComparisonService service)
    : IRequestHandler<CompareListingsCommand, IResult>
{
    public Task<IResult> Handle(CompareListingsCommand command, CancellationToken cancellationToken)
        => Task.FromResult(service.Compare(command.Request).ToHttpResult());
}
