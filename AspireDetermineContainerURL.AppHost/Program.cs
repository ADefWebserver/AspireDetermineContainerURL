var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.AspireDetermineContainerURL_Web>("webfrontend")
    .WithExternalHttpEndpoints();

// After Aspire resolves endpoints, inject our own “APP_URL” env var
// to the webfrontend container.
// This is used by the webfrontend container to determine its own URL.
web.WithEnvironment("APP_URL", () => web.GetEndpoint("https").Url);

builder.Build().Run();
