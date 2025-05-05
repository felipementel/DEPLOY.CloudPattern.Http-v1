using DEPLOY.Service.Function;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        //services.TryAddSingleton<IChaosManager, ChaosManager>(); // <-- Add this line
        //services.AddHttpContextAccessor(); // <-- Add this line
    })
    .Build();

await host.RunAsync();
