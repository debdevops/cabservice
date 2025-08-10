using CabService.NotificationService.DTOs;
using CabService.NotificationService.Services;
using CabService.Shared.Events;
using CabService.Shared.Models;

namespace CabService.NotificationService.EventHandlers;

public class DriverEventHandler
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DriverEventHandler> _logger;

    public DriverEventHandler(
        INotificationService notificationService,
        ILogger<DriverEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleDriverRegisteredAsync(DriverRegisteredEvent driverEvent)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = driverEvent.DriverId,
                Title = "Welcome to Cab Service!",
                Message = "Your driver registration has been completed. You can now start accepting ride requests.",
                Type = NotificationType.AccountUpdate,
                Channel = NotificationChannel.Email,
                RelatedEntityId = driverEvent.DriverId,
                RelatedEntityType = "Driver"
            });

            _logger.LogInformation("Sent driver registration notification for driver {DriverId}", driverEvent.DriverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling driver registered event for driver {DriverId}", driverEvent.DriverId);
        }
    }

    public async Task HandleDriverStatusChangedAsync(DriverStatusChangedEvent statusEvent)
    {
        try
        {
            var message = statusEvent.NewStatus switch
            {
                DriverStatus.Available => "You are now online and can receive ride requests.",
                DriverStatus.Offline => "You are now offline and will not receive ride requests.",
                DriverStatus.Busy => "Your status has been set to busy.",
                _ => $"Your status has been updated to {statusEvent.NewStatus}."
            };

            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = statusEvent.DriverId,
                Title = "Status Updated",
                Message = message,
                Type = NotificationType.StatusUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = statusEvent.DriverId,
                RelatedEntityType = "Driver"
            });

            _logger.LogInformation("Sent driver status change notification for driver {DriverId}", statusEvent.DriverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling driver status changed event for driver {DriverId}", statusEvent.DriverId);
        }
    }

    public async Task HandleDriverWentOnlineAsync(DriverWentOnlineEvent onlineEvent)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = onlineEvent.DriverId,
                Title = "You're Online!",
                Message = "You are now online and ready to receive ride requests in your area.",
                Type = NotificationType.StatusUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = onlineEvent.DriverId,
                RelatedEntityType = "Driver"
            });

            _logger.LogInformation("Sent driver went online notification for driver {DriverId}", onlineEvent.DriverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling driver went online event for driver {DriverId}", onlineEvent.DriverId);
        }
    }

    public async Task HandleDriverWentOfflineAsync(DriverWentOfflineEvent offlineEvent)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = offlineEvent.DriverId,
                Title = "You're Offline",
                Message = "You are now offline and will not receive any new ride requests.",
                Type = NotificationType.StatusUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = offlineEvent.DriverId,
                RelatedEntityType = "Driver"
            });

            _logger.LogInformation("Sent driver went offline notification for driver {DriverId}", offlineEvent.DriverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling driver went offline event for driver {DriverId}", offlineEvent.DriverId);
        }
    }
}
