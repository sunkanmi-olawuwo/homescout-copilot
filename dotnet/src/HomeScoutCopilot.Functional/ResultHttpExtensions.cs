using FluentResults;
using Microsoft.AspNetCore.Http;

namespace HomeScoutCopilot.Functional;

/// <summary>
/// Maps FluentResults results to HTTP responses so endpoints stay thin and expected
/// failures become ProblemDetails instead of exceptions.
/// </summary>
public static class ResultHttpExtensions
{
    private const string FailureTitle = "Request could not be completed";

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Problem(result);

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : Problem(result);

    private static IResult Problem(ResultBase result) =>
        Results.Problem(
            title: FailureTitle,
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: StatusCodes.Status400BadRequest);
}
