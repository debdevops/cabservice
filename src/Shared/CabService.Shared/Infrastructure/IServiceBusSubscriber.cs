using CabService.Shared.Events;

namespace CabService.Shared.Infrastructure;

public interface IServiceBusSubscriber
{
    Task HandleAsync<T>(T eventData) where T : BaseEvent;
}
