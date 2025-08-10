using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CabService.BookingService.EventHandlers;

public class PaymentEventHandler : IServiceBusSubscriber
{
    private readonly ILogger<PaymentEventHandler> _logger;

    public PaymentEventHandler(ILogger<PaymentEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync<T>(T eventData) where T : BaseEvent
    {
        _logger.LogInformation("Processing payment event: {EventType}", typeof(T).Name);

        switch (eventData)
        {
            case PaymentProcessedEvent paymentProcessed:
                await HandlePaymentProcessedAsync(paymentProcessed);
                break;
            case PaymentFailedEvent paymentFailed:
                await HandlePaymentFailedAsync(paymentFailed);
                break;
            case RefundProcessedEvent refundProcessed:
                await HandleRefundProcessedAsync(refundProcessed);
                break;
            default:
                _logger.LogWarning("Unhandled event type: {EventType}", typeof(T).Name);
                break;
        }
    }

    private async Task HandlePaymentProcessedAsync(PaymentProcessedEvent eventData)
    {
        _logger.LogInformation("Handling payment processed event for booking {BookingId}", eventData.BookingId);
        // Booking service logic for payment processed - complete booking
        await Task.CompletedTask;
    }

    private async Task HandlePaymentFailedAsync(PaymentFailedEvent eventData)
    {
        _logger.LogInformation("Handling payment failed event for booking {BookingId}", eventData.BookingId);
        // Booking service logic for payment failed - cancel booking
        await Task.CompletedTask;
    }

    private async Task HandleRefundProcessedAsync(RefundProcessedEvent eventData)
    {
        _logger.LogInformation("Handling refund processed event for booking {BookingId}", eventData.BookingId);
        // Booking service logic for refund processed - update booking status
        await Task.CompletedTask;
    }
}
