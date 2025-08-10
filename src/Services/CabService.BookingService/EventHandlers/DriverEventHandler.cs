using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CabService.BookingService.EventHandlers;

public class DriverEventHandler : IServiceBusSubscriber
{
    private readonly ILogger<DriverEventHandler> _logger;

    public DriverEventHandler(ILogger<DriverEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync<T>(T eventData) where T : BaseEvent
    {
        _logger.LogInformation("Processing driver event: {EventType}", typeof(T).Name);

        switch (eventData)
        {
            case DriverLocationUpdatedEvent driverLocationUpdated:
                await HandleDriverLocationUpdatedAsync(driverLocationUpdated);
                break;
            case DriverStatusChangedEvent driverStatusChanged:
                await HandleDriverStatusChangedAsync(driverStatusChanged);
                break;
            default:
                _logger.LogWarning("Unhandled event type: {EventType}", typeof(T).Name);
                break;
        }
    }

    private async Task HandleDriverLocationUpdatedAsync(DriverLocationUpdatedEvent eventData)
    {
        _logger.LogInformation("Handling driver location updated event for driver {DriverId}", eventData.DriverId);
        // Booking service logic for driver location updated - update ongoing bookings
        await Task.CompletedTask;
    }

    private async Task HandleDriverStatusChangedAsync(DriverStatusChangedEvent eventData)
    {
        _logger.LogInformation("Handling driver status changed event for driver {DriverId}", eventData.DriverId);
        // Booking service logic for driver status changed - handle availability changes
        await Task.CompletedTask;
    }
}
