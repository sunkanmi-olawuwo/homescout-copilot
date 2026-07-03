var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.HomeScoutCopilot_API>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../../frontend")
    .WithPnpm()
    .WithReference(apiService)
    .WaitFor(apiService);

apiService.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
