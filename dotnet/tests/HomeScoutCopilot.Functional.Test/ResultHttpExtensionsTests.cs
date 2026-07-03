using FluentResults;
using HomeScoutCopilot.Functional;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HomeScoutCopilot.Functional.Test;

public class ResultHttpExtensionsTests
{
    private static async Task<int> ExecuteStatusAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }

    [Test]
    public async Task Ok_result_maps_to_200()
    {
        var result = Result.Ok("value");

        Assert.That(await ExecuteStatusAsync(result.ToHttpResult()), Is.EqualTo(200));
    }

    [Test]
    public async Task Failed_result_maps_to_400_problem()
    {
        var result = Result.Fail<string>("something went wrong");

        Assert.That(await ExecuteStatusAsync(result.ToHttpResult()), Is.EqualTo(400));
    }

    [Test]
    public async Task Ok_non_generic_result_maps_to_204()
    {
        var result = Result.Ok();

        Assert.That(await ExecuteStatusAsync(result.ToHttpResult()), Is.EqualTo(204));
    }
}
