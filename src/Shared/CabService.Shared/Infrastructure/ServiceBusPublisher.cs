using Azure.Messaging.ServiceBus;
using CabService.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CabService.Shared.Infrastructure;

public class ServiceBusPublisher : IServiceBusPublisher, IDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();
    private bool _disposed = false;

    public ServiceBusPublisher(IConfiguration configuration, ILogger<ServiceBusPublisher> logger)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus") 
            ?? throw new InvalidOperationException("ServiceBus connection string is not configured");
        
        _client = new ServiceBusClient(connectionString);
        _logger = logger;
    }

    public async Task PublishAsync<T>(T eventMessage, string topicName, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        await PublishAsync(eventMessage, topicName, eventMessage.CorrelationId, cancellationToken);
    }

    public async Task PublishAsync<T>(T eventMessage, string topicName, string correlationId, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        try
        {
            var sender = await GetSenderAsync(topicName);
            
            eventMessage.CorrelationId = correlationId;
            var messageBody = JsonConvert.SerializeObject(eventMessage);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
            {
                MessageId = eventMessage.Id,
                CorrelationId = correlationId,
                Subject = eventMessage.EventType,
                ContentType = "application/json"
            };

            message.ApplicationProperties.Add("EventType", eventMessage.EventType);
            message.ApplicationProperties.Add("Source", eventMessage.Source);
            message.ApplicationProperties.Add("Version", eventMessage.Version);

            await sender.SendMessageAsync(message, cancellationToken);
            
            _logger.LogInformation("Published event {EventType} with ID {EventId} to topic {TopicName}", 
                eventMessage.EventType, eventMessage.Id, topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with ID {EventId} to topic {TopicName}", 
                eventMessage.EventType, eventMessage.Id, topicName);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> eventMessages, string topicName, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        try
        {
            var sender = await GetSenderAsync(topicName);
            var messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var eventMessage in eventMessages)
            {
                var messageBody = JsonConvert.SerializeObject(eventMessage);
                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
                {
                    MessageId = eventMessage.Id,
                    CorrelationId = eventMessage.CorrelationId,
                    Subject = eventMessage.EventType,
                    ContentType = "application/json"
                };

                message.ApplicationProperties.Add("EventType", eventMessage.EventType);
                message.ApplicationProperties.Add("Source", eventMessage.Source);
                message.ApplicationProperties.Add("Version", eventMessage.Version);

                if (!messageBatch.TryAddMessage(message))
                {
                    // Send the current batch and create a new one
                    await sender.SendMessagesAsync(messageBatch, cancellationToken);
                    messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);
                    
                    if (!messageBatch.TryAddMessage(message))
                    {
                        throw new InvalidOperationException($"Message is too large to fit in batch: {eventMessage.Id}");
                    }
                }
            }

            if (messageBatch.Count > 0)
            {
                await sender.SendMessagesAsync(messageBatch, cancellationToken);
            }

            _logger.LogInformation("Published batch of {Count} events to topic {TopicName}", 
                eventMessages.Count(), topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of events to topic {TopicName}", topicName);
            throw;
        }
    }

    private Task<ServiceBusSender> GetSenderAsync(string topicName)
    {
        if (!_senders.TryGetValue(topicName, out var sender))
        {
            sender = _client.CreateSender(topicName);
            _senders[topicName] = sender;
        }

        return Task.FromResult(sender);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var sender in _senders.Values)
            {
                sender?.DisposeAsync().AsTask().Wait();
            }
            _senders.Clear();
            
            _client?.DisposeAsync().AsTask().Wait();
            _disposed = true;
        }
    }
}
