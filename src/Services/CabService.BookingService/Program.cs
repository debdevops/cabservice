using CabService.Shared.Extensions;
using CabService.BookingService.Services;
using CabService.BookingService.EventHandlers;
using CabService.Shared.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add shared services
builder.Services.AddSharedServices(builder.Configuration);

// Add service-specific services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDriverMatchingService, DriverMatchingService>();
builder.Services.AddScoped<IFareCalculationService, FareCalculationService>();
builder.Services.AddAutoMapper(typeof(Program));

// Add Service Bus event handlers
builder.Services.AddScoped<IServiceBusSubscriber, DriverEventHandler>();
builder.Services.AddScoped<IServiceBusSubscriber, PaymentEventHandler>();
builder.Services.AddSingleton<ServiceBusBackgroundService>(provider =>
{
    var serviceBusClient = provider.GetRequiredService<ServiceBusClient>();
    var logger = provider.GetRequiredService<ILogger<ServiceBusBackgroundService>>();
    return new ServiceBusBackgroundService(provider, logger, serviceBusClient, "booking-service");
});
builder.Services.AddHostedService<ServiceBusBackgroundService>(provider => 
    provider.GetRequiredService<ServiceBusBackgroundService>());

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { }
