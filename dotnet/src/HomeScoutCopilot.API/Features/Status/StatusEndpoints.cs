using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Status;

public sealed class StatusEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGet("/api/status", (IMediator mediator) => mediator.Send(GetStatusQuery.Instance))
            .WithName("GetStatus")
            .WithTags("Status")
            .WithSummary("Product status and direction")
            .Produces<HomeScoutStatus>();
}

public sealed record GetStatusQuery : IRequest<IResult>
{
    public static GetStatusQuery Instance { get; } = new();
}

public sealed class GetStatusHandler(IHomeScoutService service) : IRequestHandler<GetStatusQuery, IResult>
{
    public Task<IResult> Handle(GetStatusQuery request, CancellationToken cancellationToken)
        => Task.FromResult(service.GetStatus().ToHttpResult());
}
