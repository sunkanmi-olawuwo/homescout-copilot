using System.Text.Json.Serialization;
using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;
using HomeScoutCopilot.Shared.Application.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IHomeScoutService, HomeScoutService>();
builder.Services.AddSingleton<IMortgageCostEstimator, MortgageCostEstimator>();

// Accept/emit enums (e.g. RepaymentType) as strings for friendlier JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMemoryCache();
builder.Services.AddOptions<BaseRateOptions>()
    .Bind(builder.Configuration.GetSection(BaseRateOptions.SectionName));
builder.Services.AddHttpClient<IBaseRateProvider, BankOfEnglandBaseRateProvider>(client =>
{
    // A descriptive User-Agent; the BoE endpoint rejects requests without one.
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "HomeScoutCopilot/1.0 (+https://github.com/sunkanmi-olawuwo/homescout-copilot)");
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/status", (IHomeScoutService service) => service.GetStatus().ToHttpResult());

api.MapGet("/comparison/sample", (IHomeScoutService service) => service.GetComparisonSample().ToHttpResult());

// Deterministic mortgage estimate from the buyer's own figures. Invalid input becomes
// a 400 ProblemDetails via the FluentResults mapping — not an exception.
api.MapPost("/mortgage/estimate", (MortgageEstimateRequest request, IMortgageCostEstimator estimator)
    => estimator.Estimate(request).ToHttpResult());

// Base rate is orienting context only (never a mortgage product rate); the provider
// never throws, so this endpoint always returns 200 with a live/cache/fallback value.
api.MapGet("/mortgage/base-rate", async (IBaseRateProvider baseRate, CancellationToken cancellationToken)
    => Results.Ok(await baseRate.GetCurrentAsync(cancellationToken)));

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
