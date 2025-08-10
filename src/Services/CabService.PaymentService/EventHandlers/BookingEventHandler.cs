using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CabService.PaymentService.EventHandlers;

public class BookingEventHandler : IServiceBusSubscriber
{
    private readonly ILogger<BookingEventHandler> _logger;

    public BookingEventHandler(ILogger<BookingEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync<T>(T eventData) where T : BaseEvent
    {
        _logger.LogInformation("Processing booking event: {EventType}", typeof(T).Name);

        switch (eventData)
        {
            case BookingCreatedEvent bookingCreated:
                await HandleBookingCreatedAsync(bookingCreated);
                break;
            case BookingCancelledEvent bookingCancelled:
                await HandleBookingCancelledAsync(bookingCancelled);
                break;
            case BookingCompletedEvent bookingCompleted:
                await HandleBookingCompletedAsync(bookingCompleted);
                break;
            default:
                _logger.LogWarning("Unhandled event type: {EventType}", typeof(T).Name);
                break;
        }
    }

    private async Task HandleBookingCreatedAsync(BookingCreatedEvent eventData)
    {
        _logger.LogInformation("Handling booking created event for booking {BookingId}", eventData.BookingId);
        // Payment service logic for booking created - prepare for payment processing
        await Task.CompletedTask;
    }

    private async Task HandleBookingCancelledAsync(BookingCancelledEvent eventData)
    {
        _logger.LogInformation("Handling booking cancelled event for booking {BookingId}", eventData.BookingId);
        // Payment service logic for booking cancelled - process refunds if needed
        await Task.CompletedTask;
    }

    private async Task HandleBookingCompletedAsync(BookingCompletedEvent eventData)
    {
        _logger.LogInformation("Handling booking completed event for booking {BookingId}", eventData.BookingId);
        // Payment service logic for booking completed - finalize payment
        await Task.CompletedTask;
    }
}
