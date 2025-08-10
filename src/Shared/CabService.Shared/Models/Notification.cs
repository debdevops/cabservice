using System.ComponentModel.DataAnnotations;

namespace CabService.Shared.Models;

public class Notification : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    
    public NotificationChannel Channel { get; set; }
    
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    public string? RelatedEntityId { get; set; }
    
    public string? RelatedEntityType { get; set; }
    
    public Dictionary<string, object> Data { get; set; } = new();
    
    public string? FailureReason { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? ScheduledAt { get; set; }
}

public enum NotificationType
{
    BookingConfirmation,
    DriverAssigned,
    DriverEnRoute,
    DriverArrived,
    TripStarted,
    TripCompleted,
    PaymentProcessed,
    PaymentFailed,
    TripCancelled,
    RatingRequest,
    PromotionalOffer,
    SystemAlert,
    AccountUpdate,
    PaymentUpdate,
    StatusUpdate,
    BookingUpdate,
    TripUpdate
}

public enum NotificationChannel
{
    PushNotification,
    Email,
    SMS,
    InApp
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed
}
