using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CabService.Shared.Events;

namespace CabService.Shared.Infrastructure;

public class ServiceBusBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceBusBackgroundService> _logger;
    private readonly ServiceBusProcessor _processor;

    public ServiceBusBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ServiceBusBackgroundService> logger,
        ServiceBusClient serviceBusClient,
        string subscriptionName)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Create processor for the subscription
        _processor = serviceBusClient.CreateProcessor("cab-events", subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });
        
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        await _processor.StopProcessingAsync();
    }

    private BaseEvent? DeserializeEvent(string messageBody, string eventType)
    {
        try
        {
            return eventType switch
            {
                "BookingRequestedEvent" => JsonSerializer.Deserialize<BookingRequestedEvent>(messageBody),
                "BookingCreatedEvent" => JsonSerializer.Deserialize<BookingCreatedEvent>(messageBody),
                "BookingCompletedEvent" => JsonSerializer.Deserialize<BookingCompletedEvent>(messageBody),
                "DriverAssignedEvent" => JsonSerializer.Deserialize<DriverAssignedEvent>(messageBody),
                "DriverEnRouteEvent" => JsonSerializer.Deserialize<DriverEnRouteEvent>(messageBody),
                "DriverArrivedEvent" => JsonSerializer.Deserialize<DriverArrivedEvent>(messageBody),
                "TripStartedEvent" => JsonSerializer.Deserialize<TripStartedEvent>(messageBody),
                "TripCompletedEvent" => JsonSerializer.Deserialize<TripCompletedEvent>(messageBody),
                "BookingCancelledEvent" => JsonSerializer.Deserialize<BookingCancelledEvent>(messageBody),
                "PaymentInitiatedEvent" => JsonSerializer.Deserialize<PaymentInitiatedEvent>(messageBody),
                "PaymentProcessedEvent" => JsonSerializer.Deserialize<PaymentProcessedEvent>(messageBody),
                "PaymentFailedEvent" => JsonSerializer.Deserialize<PaymentFailedEvent>(messageBody),
                "RefundProcessedEvent" => JsonSerializer.Deserialize<RefundProcessedEvent>(messageBody),
                "DriverRegisteredEvent" => JsonSerializer.Deserialize<DriverRegisteredEvent>(messageBody),
                "DriverLocationUpdatedEvent" => JsonSerializer.Deserialize<DriverLocationUpdatedEvent>(messageBody),
                "DriverStatusChangedEvent" => JsonSerializer.Deserialize<DriverStatusChangedEvent>(messageBody),
                _ => null // Unknown event type
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event of type {EventType}", eventType);
            return null;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            var eventType = args.Message.Subject;
            
            _logger.LogInformation("Received message of type {EventType}: {Message}", eventType, messageBody);

            using var scope = _serviceProvider.CreateScope();
            var subscribers = scope.ServiceProvider.GetServices<IServiceBusSubscriber>();
            
            foreach (var subscriber in subscribers)
            {
                try
                {
                    // Deserialize to the correct event type based on the Subject
                    var eventData = DeserializeEvent(messageBody, eventType);
                    if (eventData != null)
                    {
                        await subscriber.HandleAsync(eventData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with subscriber {SubscriberType}", subscriber.GetType().Name);
                }
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error occurred");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _processor?.DisposeAsync().AsTask().Wait();
        base.Dispose();
    }
}
