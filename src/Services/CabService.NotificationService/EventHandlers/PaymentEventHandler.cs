using CabService.NotificationService.DTOs;
using CabService.NotificationService.Services;
using CabService.Shared.Events;
using CabService.Shared.Models;

namespace CabService.NotificationService.EventHandlers;

public class PaymentEventHandler
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentEventHandler> _logger;

    public PaymentEventHandler(
        INotificationService notificationService,
        ILogger<PaymentEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent paymentEvent)
    {
        try
        {
            var message = paymentEvent.Status == PaymentStatus.Completed 
                ? $"Payment of ${paymentEvent.Amount:F2} has been processed successfully."
                : $"Payment of ${paymentEvent.Amount:F2} failed. Please try again.";

            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = paymentEvent.UserId,
                Title = paymentEvent.Status == PaymentStatus.Completed ? "Payment Successful" : "Payment Failed",
                Message = message,
                Type = NotificationType.PaymentUpdate,
                Channel = NotificationChannel.PushNotification,
                RelatedEntityId = paymentEvent.PaymentId,
                RelatedEntityType = "Payment"
            });

            _logger.LogInformation("Sent payment processed notification for payment {PaymentId}", paymentEvent.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment processed event for payment {PaymentId}", paymentEvent.PaymentId);
        }
    }

    public async Task HandleRefundProcessedAsync(RefundProcessedEvent refundEvent)
    {
        try
        {
            var message = refundEvent.Status == RefundStatus.Completed 
                ? $"Refund of ${refundEvent.Amount:F2} has been processed successfully."
                : $"Refund of ${refundEvent.Amount:F2} failed. Please contact support.";

            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = refundEvent.UserId,
                Title = refundEvent.Status == RefundStatus.Completed ? "Refund Processed" : "Refund Failed",
                Message = message,
                Type = NotificationType.PaymentUpdate,
                Channel = NotificationChannel.Email,
                RelatedEntityId = refundEvent.RefundId,
                RelatedEntityType = "Refund"
            });

            _logger.LogInformation("Sent refund processed notification for refund {RefundId}", refundEvent.RefundId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling refund processed event for refund {RefundId}", refundEvent.RefundId);
        }
    }
}
