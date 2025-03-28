using AspireDetermineContainerURL.Web;
using AspireDetermineContainerURL.Web.Components;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

var app = builder.Build();

// ****************************
// Get the URL of the Container
// ****************************
if (app.Environment.IsDevelopment())
{
    app.Configuration["DetectedEnvironment"] = "Dev";

    // Get value from Aspire
    // This was set in AppHost
    var LocalUrl = builder.Configuration["APP_URL"];

    // Add the URL to the configuration so it can be displayed in the .razor apge
    app.Configuration["Aspire_URL"] = LocalUrl;    
}
else
{
    app.Configuration["DetectedEnvironment"] = "Production";

    // Get values from Azure
    // Retrieve required environment variables
    var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
    var resourceGroupName = Environment.GetEnvironmentVariable("RESOURCE_GROUP");
    var configStoreName = Environment.GetEnvironmentVariable("APPCONFIGURATION_NAME");

    if (!string.IsNullOrEmpty(subscriptionId) &&
        !string.IsNullOrEmpty(resourceGroupName) &&
        !string.IsNullOrEmpty(configStoreName))
    {
        // Create an ArmClient using DefaultAzureCredential
        ArmClient armClient = new ArmClient(new DefaultAzureCredential());

        // Build the resource identifier for your App Configuration store
        ResourceIdentifier configStoreId = AppConfigurationStoreResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, configStoreName);

        // Retrieve the App Configuration store resource using the extension method
        AppConfigurationStoreResource configStore = armClient.GetAppConfigurationStoreResource(configStoreId);

        // Fetch the configuration store details
        var response = await configStore.GetAsync();
        var storeData = response.Value.Data;

        // Retrieve the endpoint for the configuration store
        string endpoint = storeData.Endpoint;

        if (string.IsNullOrEmpty(endpoint))
        {
            Console.WriteLine("No endpoint information available for the configuration store.");

            // Return values found
            string? ParamSubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            string? ParamResourceGroupName = Environment.GetEnvironmentVariable("RESOURCE_GROUP");
            string? ParamConfigStoreName = Environment.GetEnvironmentVariable("APPCONFIGURATION_NAME");

            app.Configuration["AzureContainerApps_URL"] = $"AZURE_SUBSCRIPTION_ID:{ParamSubscriptionId} " +
                $"- RESOURCE_GROUP: {ParamResourceGroupName} " +
                $"- APPCONFIGURATION_NAME: {ParamConfigStoreName}";

            return;
        }

        // Add the URL to the configuration so it can be displayed in the .razor apge
        app.Configuration["AzureContainerApps_URL"] = endpoint;
    }
    else
    {
        Console.WriteLine("Please set the following environment variables:");
        Console.WriteLine("AZURE_SUBSCRIPTION_ID, RESOURCE_GROUP, APPCONFIGURATION_NAME");

        // Return values found
        string? ParamSubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        string? ParamResourceGroupName = Environment.GetEnvironmentVariable("RESOURCE_GROUP");
        string? ParamConfigStoreName = Environment.GetEnvironmentVariable("APPCONFIGURATION_NAME");

        app.Configuration["AzureContainerApps_URL"] = $"AZURE_SUBSCRIPTION_ID:{ParamSubscriptionId} " +
            $"- RESOURCE_GROUP: {ParamResourceGroupName} " +
            $"- APPCONFIGURATION_NAME: {ParamConfigStoreName}";
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
