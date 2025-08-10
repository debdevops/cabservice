using CabService.NotificationService.DTOs;
using CabService.NotificationService.Services;
using CabService.Shared.Events;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;

namespace CabService.NotificationService.EventHandlers;

public class BookingEventHandler : IServiceBusSubscriber
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<BookingEventHandler> _logger;

    public BookingEventHandler(
        INotificationService notificationService,
        ILogger<BookingEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync<T>(T eventData) where T : BaseEvent
    {
        _logger.LogInformation("Processing booking event: {EventType}", typeof(T).Name);

        switch (eventData)
        {
            case BookingRequestedEvent bookingRequested:
                await HandleBookingRequestedAsync(bookingRequested);
                break;
            case BookingAcceptedEvent bookingAccepted:
                await HandleBookingAcceptedAsync(bookingAccepted);
                break;
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

    private async Task HandleBookingCreatedAsync(BookingCreatedEvent bookingEvent)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.PassengerId,
                Title = "Booking Created",
                Message = $"Your booking has been created and we're finding you a driver.",
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });
            _logger.LogInformation("Sent booking created notification for booking {BookingId}", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking created event for booking {BookingId}", bookingEvent.BookingId);
        }
    }



    private async Task HandleBookingCompletedAsync(BookingCompletedEvent bookingEvent)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.PassengerId,
                Title = "Trip Completed",
                Message = "Your trip has been completed successfully.",
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });
            _logger.LogInformation("Sent booking completed notification for booking {BookingId}", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking completed event for booking {BookingId}", bookingEvent.BookingId);
        }
    }

    public async Task HandleBookingRequestedAsync(BookingRequestedEvent bookingEvent)
    {
        try
        {
            // Notify passenger
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.PassengerId,
                Title = "Booking Requested",
                Message = $"Your booking request from {bookingEvent.PickupLocation} to {bookingEvent.DropoffLocation} has been received. We're finding you a driver.",
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            _logger.LogInformation("Sent booking requested notification for booking {BookingId}", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking requested event for booking {BookingId}", bookingEvent.BookingId);
        }
    }

    public async Task HandleBookingAcceptedAsync(BookingAcceptedEvent bookingEvent)
    {
        try
        {
            // Notify passenger
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.PassengerId,
                Title = "Driver Found!",
                Message = $"Your driver {bookingEvent.DriverName} is on the way. Vehicle: {bookingEvent.VehicleDetails}",
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.PushNotification,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            // Notify driver
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.DriverId,
                Title = "New Booking Accepted",
                Message = $"You have accepted a booking. Pickup location: {bookingEvent.PickupLocation}",
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            _logger.LogInformation("Sent booking accepted notifications for booking {BookingId}", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking accepted event for booking {BookingId}", bookingEvent.BookingId);
        }
    }

    public async Task HandleTripStartedAsync(TripStartedEvent tripEvent)
    {
        try
        {
            // Notify passenger
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = tripEvent.PassengerId,
                Title = "Trip Started",
                Message = "Your trip has started. Enjoy your ride!",
                Type = NotificationType.TripUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = tripEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            _logger.LogInformation("Sent trip started notification for booking {BookingId}", tripEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling trip started event for booking {BookingId}", tripEvent.BookingId);
        }
    }

    public async Task HandleTripCompletedAsync(TripCompletedEvent tripEvent)
    {
        try
        {
            // Notify passenger
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = tripEvent.PassengerId,
                Title = "Trip Completed",
                Message = $"Your trip has been completed. Total fare: ${tripEvent.TotalFare:F2}. Please rate your experience.",
                Type = NotificationType.TripUpdate,
                Channel = NotificationChannel.PushNotification,
                RelatedEntityId = tripEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            // Notify driver
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = tripEvent.DriverId,
                Title = "Trip Completed",
                Message = $"Trip completed successfully. Earnings: ${tripEvent.DriverEarnings:F2}",
                Type = NotificationType.TripUpdate,
                Channel = NotificationChannel.InApp,
                RelatedEntityId = tripEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            _logger.LogInformation("Sent trip completed notifications for booking {BookingId}", tripEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling trip completed event for booking {BookingId}", tripEvent.BookingId);
        }
    }

    public async Task HandleBookingCancelledAsync(BookingCancelledEvent bookingEvent)
    {
        try
        {
            var message = bookingEvent.CancelledBy == "passenger" 
                ? "You have cancelled your booking."
                : $"Your booking has been cancelled by the {bookingEvent.CancelledBy}.";

            // Notify passenger
            await _notificationService.SendNotificationAsync(new SendNotificationDto
            {
                UserId = bookingEvent.PassengerId,
                Title = "Booking Cancelled",
                Message = message,
                Type = NotificationType.BookingUpdate,
                Channel = NotificationChannel.PushNotification,
                RelatedEntityId = bookingEvent.BookingId,
                RelatedEntityType = "Booking"
            });

            // Notify driver if assigned
            if (!string.IsNullOrEmpty(bookingEvent.DriverId))
            {
                await _notificationService.SendNotificationAsync(new SendNotificationDto
                {
                    UserId = bookingEvent.DriverId,
                    Title = "Booking Cancelled",
                    Message = $"The booking has been cancelled by the {bookingEvent.CancelledBy}.",
                    Type = NotificationType.BookingUpdate,
                    Channel = NotificationChannel.InApp,
                    RelatedEntityId = bookingEvent.BookingId,
                    RelatedEntityType = "Booking"
                });
            }

            _logger.LogInformation("Sent booking cancelled notifications for booking {BookingId}", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling booking cancelled event for booking {BookingId}", bookingEvent.BookingId);
        }
    }
}
