var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.HomeScoutCopilot_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../../frontend")
    .WithReference(apiService)
    .WaitFor(apiService);

apiService.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
