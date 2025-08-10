using CabService.Shared.Extensions;
using CabService.PaymentService.Services;
using CabService.PaymentService.EventHandlers;
using CabService.Shared.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Azure.Messaging.ServiceBus;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add shared services
builder.Services.AddSharedServices(builder.Configuration);

// Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Add service-specific services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();
builder.Services.AddScoped<IRefundService, CabService.PaymentService.Services.RefundService>();
builder.Services.AddAutoMapper(typeof(Program));

// Add Service Bus event handlers
builder.Services.AddScoped<IServiceBusSubscriber, BookingEventHandler>();
builder.Services.AddSingleton<ServiceBusBackgroundService>(provider =>
{
    var serviceBusClient = provider.GetRequiredService<ServiceBusClient>();
    var logger = provider.GetRequiredService<ILogger<ServiceBusBackgroundService>>();
    return new ServiceBusBackgroundService(provider, logger, serviceBusClient, "payment-service");
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
