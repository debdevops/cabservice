using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CabService.Shared.Extensions;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add shared services
        services.AddSharedServices(context.Configuration);
        
        // Add HTTP client
        services.AddHttpClient();
        
        // Add AutoMapper
        services.AddAutoMapper(typeof(Program));
    })
    .Build();

host.Run();
