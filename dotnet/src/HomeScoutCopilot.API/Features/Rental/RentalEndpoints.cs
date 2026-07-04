using Carter;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Contracts;
using MediatR;

namespace HomeScoutCopilot.API.Features.Rental;

public sealed class RentalEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapPost("/api/rental/estimate", (IMediator mediator, RentalCostRequest request)
                => mediator.Send(new EstimateRentalCostCommand(request)))
            .WithName("EstimateRentalCost")
            .WithTags("Rental")
            .WithSummary("Estimate the true monthly + upfront cost of renting from the listing's figures")
            .Produces<RentalCostResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed record EstimateRentalCostCommand(RentalCostRequest Request) : IRequest<IResult>;

public sealed class EstimateRentalCostHandler(IRentalCostEstimator estimator)
    : IRequestHandler<EstimateRentalCostCommand, IResult>
{
    public Task<IResult> Handle(EstimateRentalCostCommand command, CancellationToken cancellationToken)
        => Task.FromResult(estimator.Estimate(command.Request).ToHttpResult());
}
