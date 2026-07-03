using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Comparison;

public sealed class ComparisonEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("/api/comparison/sample", (IMediator mediator) => mediator.Send(GetComparisonSampleQuery.Instance))
            .WithName("GetComparisonSample")
            .WithTags("Comparison")
            .WithSummary("Placeholder sample comparison")
            .Produces<ComparisonSample>();
}

public sealed record GetComparisonSampleQuery : IRequest<IResult>
{
    public static GetComparisonSampleQuery Instance { get; } = new();
}

public sealed class GetComparisonSampleHandler(IHomeScoutService service)
    : IRequestHandler<GetComparisonSampleQuery, IResult>
{
    public Task<IResult> Handle(GetComparisonSampleQuery request, CancellationToken cancellationToken)
        => Task.FromResult(service.GetComparisonSample().ToHttpResult());
}
