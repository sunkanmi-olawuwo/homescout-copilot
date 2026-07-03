using HomeScoutCopilot.API.Service;
using HomeScoutCopilot.Functional;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IHomeScoutService, HomeScoutService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/status", (IHomeScoutService service) => service.GetStatus().ToHttpResult());

api.MapGet("/comparison/sample", (IHomeScoutService service) => service.GetComparisonSample().ToHttpResult());

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
