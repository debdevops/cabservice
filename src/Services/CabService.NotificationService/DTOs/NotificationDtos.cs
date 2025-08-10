using System.ComponentModel.DataAnnotations;
using CabService.Shared.Models;

namespace CabService.NotificationService.DTOs;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SendNotificationDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    public string? RelatedEntityId { get; set; }
    
    public string? RelatedEntityType { get; set; }
    
    public Dictionary<string, object> Data { get; set; } = new();
    
    public DateTime? ScheduledAt { get; set; }
}

public class SendBulkNotificationDto
{
    [Required]
    public List<string> UserIds { get; set; } = new();
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    public string? RelatedEntityId { get; set; }
    
    public string? RelatedEntityType { get; set; }
    
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ScheduleNotificationDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    [Required]
    public DateTime ScheduledAt { get; set; }
    
    public string? RelatedEntityId { get; set; }
    
    public string? RelatedEntityType { get; set; }
    
    public Dictionary<string, object> Data { get; set; } = new();
}

public class NotificationTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public Dictionary<string, object> Variables { get; set; } = new();
    public bool IsActive { get; set; }
}

public class EmailNotificationDto
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment> Attachments { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class SmsNotificationDto
{
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PushNotificationDto
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Icon { get; set; }
    public string? Sound { get; set; }
    public string? ImageUrl { get; set; }
    public int? Badge { get; set; }
}

public class NotificationPreferencesDto
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public Dictionary<NotificationType, bool> TypePreferences { get; set; } = new();
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
}

public class UserNotificationPreferencesDto
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public Dictionary<NotificationType, bool> TypePreferences { get; set; } = new();
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
}

public class UserNotificationPreferences
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public Dictionary<NotificationType, bool> TypePreferences { get; set; } = new();
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
}

public class DeviceTokenDto
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string Type { get; set; } = string.Empty;
}
