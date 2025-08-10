using CabService.NotificationService.DTOs;

namespace CabService.NotificationService.Services;

public interface INotificationService
{
    Task<NotificationDto> SendNotificationAsync(SendNotificationDto sendNotificationDto);
    Task<NotificationDto?> GetNotificationByIdAsync(string id);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(string id);
    Task<int> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(string id);
    Task<IEnumerable<NotificationDto>> SendBulkNotificationsAsync(SendBulkNotificationDto bulkNotificationDto);
    Task<int> GetUnreadCountAsync(string userId);
    Task<NotificationDto> ScheduleNotificationAsync(ScheduleNotificationDto scheduleNotificationDto);
    Task<IEnumerable<NotificationTemplateDto>> GetNotificationTemplatesAsync();
    Task ProcessScheduledNotificationsAsync();
    Task<bool> UpdateNotificationStatusAsync(string id, Shared.Models.NotificationStatus status, string? failureReason = null);
}
