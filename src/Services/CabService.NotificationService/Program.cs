using CabService.Shared.Extensions;
using CabService.NotificationService.Services;
using CabService.NotificationService.EventHandlers;
using CabService.NotificationService.Hubs;
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

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Add service-specific services
builder.Services.AddScoped<INotificationService, CabService.NotificationService.Services.NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Add external service dependencies
builder.Services.AddSingleton<SendGrid.ISendGridClient>(provider =>
{
    var apiKey = builder.Configuration["SendGrid:ApiKey"];
    return new SendGrid.SendGridClient(apiKey);
});

// Add Firebase Admin SDK for push notifications
builder.Services.AddSingleton<FirebaseAdmin.Messaging.FirebaseMessaging>(provider =>
{
    return FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
});
builder.Services.AddAutoMapper(typeof(Program));

// Add Service Bus event handlers
builder.Services.AddScoped<BookingEventHandler>();
builder.Services.AddScoped<PaymentEventHandler>();
builder.Services.AddScoped<DriverEventHandler>();

// Add background services
builder.Services.AddHostedService<NotificationBackgroundService>();

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

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");

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
