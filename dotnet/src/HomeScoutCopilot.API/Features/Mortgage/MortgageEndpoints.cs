using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Mortgage;

public sealed class MortgageEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mortgage").WithTags("Mortgage");

        group.MapPost("/estimate", (IMediator mediator, MortgageEstimateRequest request)
                => mediator.Send(new EstimateMortgageCommand(request)))
            .WithName("EstimateMortgage")
            .WithSummary("Estimate the monthly mortgage cost from the buyer's own figures")
            .Produces<MortgageEstimateResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/base-rate", (IMediator mediator) => mediator.Send(GetBaseRateQuery.Instance))
            .WithName("GetBaseRate")
            .WithSummary("Bank of England base rate (context only)")
            .Produces<BaseRate>();
    }
}

public sealed record EstimateMortgageCommand(MortgageEstimateRequest Request) : IRequest<IResult>;

public sealed class EstimateMortgageHandler(IMortgageCostEstimator estimator)
    : IRequestHandler<EstimateMortgageCommand, IResult>
{
    public Task<IResult> Handle(EstimateMortgageCommand command, CancellationToken cancellationToken)
        => Task.FromResult(estimator.Estimate(command.Request).ToHttpResult());
}

public sealed record GetBaseRateQuery : IRequest<IResult>
{
    public static GetBaseRateQuery Instance { get; } = new();
}

public sealed class GetBaseRateHandler(IBaseRateProvider baseRate) : IRequestHandler<GetBaseRateQuery, IResult>
{
    public async Task<IResult> Handle(GetBaseRateQuery request, CancellationToken cancellationToken)
        => Results.Ok(await baseRate.GetCurrentAsync(cancellationToken));
}
