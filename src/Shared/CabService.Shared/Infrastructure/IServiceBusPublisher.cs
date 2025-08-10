using CabService.Shared.Events;

namespace CabService.Shared.Infrastructure;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(T eventMessage, string topicName, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishAsync<T>(T eventMessage, string topicName, string correlationId, CancellationToken cancellationToken = default) where T : BaseEvent;
    Task PublishBatchAsync<T>(IEnumerable<T> eventMessages, string topicName, CancellationToken cancellationToken = default) where T : BaseEvent;
}
