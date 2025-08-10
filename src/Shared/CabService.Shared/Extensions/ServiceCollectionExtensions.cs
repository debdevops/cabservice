using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using CabService.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CabService.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Cosmos DB
        services.AddCosmosDb(configuration);
        
        // Add Service Bus
        services.AddServiceBus(configuration);
        
        // Add Authentication
        services.AddAuthentication(configuration);
        
        // Add OpenTelemetry
        services.AddOpenTelemetry(configuration);
        
        // Add Health Checks
        services.AddHealthChecks(configuration);
        
        return services;
    }

    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CosmosDb");
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "CabServiceDb";

        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            return new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        });

        services.AddScoped(typeof(ICosmosRepository<>), typeof(CosmosRepository<>));

        return services;
    }

    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();
        return services;
    }

    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        services.AddAuthorization(options =>
        {
            options.AddPolicy("PassengerPolicy", policy => 
                policy.RequireClaim("role", "Passenger"));
            options.AddPolicy("DriverPolicy", policy => 
                policy.RequireClaim("role", "Driver"));
            options.AddPolicy("AdminPolicy", policy => 
                policy.RequireClaim("role", "Admin"));
        });

        return services;
    }

    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "CabService";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("CabService.*");

                // Note: Azure Monitor trace exporter will be configured at the service level
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Note: Azure Monitor Metric Exporter will be configured separately
            });

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add basic health checks
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
        
        // Add Cosmos DB health check if connection string is available
        var cosmosConnectionString = configuration.GetConnectionString("CosmosDb");
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            healthChecksBuilder.AddCheck("cosmosdb", () => HealthCheckResult.Healthy("Cosmos DB is healthy"));
        }

        // Add Service Bus health check if connection string is available
        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            healthChecksBuilder.AddCheck("servicebus", () => HealthCheckResult.Healthy("Service Bus is healthy"));
        }

        return services;
    }
}
