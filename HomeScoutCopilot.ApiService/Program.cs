var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/status", () => new HomeScoutStatus(
    Product: "HomeScout Copilot",
    Frontend: "React",
    Architecture: "API-first",
    AgentPlatform: "Microsoft Foundry Agent Service planned"));

api.MapGet("/comparison/sample", () => new ComparisonSample(
    Title: "Greenwich vs Croydon",
    Summary: "Sample placeholder for the first API-first comparison workflow."));

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

record HomeScoutStatus(string Product, string Frontend, string Architecture, string AgentPlatform);

record ComparisonSample(string Title, string Summary);
