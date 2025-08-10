using AutoMapper;
using CabService.NotificationService.DTOs;
using CabService.NotificationService.Hubs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace CabService.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly ICosmosRepository<Notification> _notificationRepository;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITemplateService _templateService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ICosmosRepository<Notification> notificationRepository,
        IServiceBusPublisher serviceBusPublisher,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushNotificationService,
        ITemplateService templateService,
        IHubContext<NotificationHub> hubContext,
        IMapper mapper,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _serviceBusPublisher = serviceBusPublisher;
        _emailService = emailService;
        _smsService = smsService;
        _pushNotificationService = pushNotificationService;
        _templateService = templateService;
        _hubContext = hubContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<NotificationDto> SendNotificationAsync(SendNotificationDto sendNotificationDto)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = sendNotificationDto.UserId,
                Title = sendNotificationDto.Title,
                Message = sendNotificationDto.Message,
                Type = sendNotificationDto.Type,
                Channel = sendNotificationDto.Channel,
                Status = NotificationStatus.Pending,
                RelatedEntityId = sendNotificationDto.RelatedEntityId,
                RelatedEntityType = sendNotificationDto.RelatedEntityType,
                Data = sendNotificationDto.Data,
                ScheduledAt = sendNotificationDto.ScheduledAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification);

            // If not scheduled, send immediately
            if (!sendNotificationDto.ScheduledAt.HasValue || sendNotificationDto.ScheduledAt <= DateTime.UtcNow)
            {
                _ = Task.Run(async () => await ProcessNotificationAsync(createdNotification.Id));
            }

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                createdNotification.Id, createdNotification.UserId);

            return _mapper.Map<NotificationDto>(createdNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", sendNotificationDto.UserId);
            throw;
        }
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(string id)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, id);
            return notification != null ? _mapper.Map<NotificationDto>(notification) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification with ID {NotificationId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, bool unreadOnly = false)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.userId = @userId AND c.isDeleted = false";
            if (unreadOnly)
            {
                query += " AND c.readAt = null";
            }
            query += " ORDER BY c.createdAt DESC OFFSET @offset LIMIT @limit";

            var parameters = new { 
                userId, 
                offset = (page - 1) * pageSize, 
                limit = pageSize 
            };
            
            var notifications = await _notificationRepository.QueryAsync(query, parameters, userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, id);
            if (notification == null || notification.ReadAt.HasValue)
            {
                return false;
            }

            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _notificationRepository.UpdateAsync(notification);

            // Send real-time update via SignalR
            await _hubContext.Clients.User(notification.UserId).SendAsync("NotificationRead", id);

            _logger.LogInformation("Marked notification {NotificationId} as read", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            throw;
        }
    }

    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.userId = @userId AND c.readAt = null AND c.isDeleted = false";
            var parameters = new { userId };
            
            var unreadNotifications = await _notificationRepository.QueryAsync(query, parameters, userId);
            var count = 0;

            foreach (var notification in unreadNotifications)
            {
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
                await _notificationRepository.UpdateAsync(notification);
                count++;
            }

            // Send real-time update via SignalR
            await _hubContext.Clients.User(userId).SendAsync("AllNotificationsRead");

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteNotificationAsync(string id)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, id);
            if (notification == null)
            {
                return false;
            }

            await _notificationRepository.DeleteAsync(id, id);

            _logger.LogInformation("Deleted notification {NotificationId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationDto>> SendBulkNotificationsAsync(SendBulkNotificationDto bulkNotificationDto)
    {
        try
        {
            var notifications = new List<Notification>();

            foreach (var userId in bulkNotificationDto.UserIds)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = bulkNotificationDto.Title,
                    Message = bulkNotificationDto.Message,
                    Type = bulkNotificationDto.Type,
                    Channel = bulkNotificationDto.Channel,
                    Status = NotificationStatus.Pending,
                    RelatedEntityId = bulkNotificationDto.RelatedEntityId,
                    RelatedEntityType = bulkNotificationDto.RelatedEntityType,
                    Data = bulkNotificationDto.Data,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                notifications.Add(notification);
            }

            var createdNotifications = new List<Notification>();
            foreach (var notification in notifications)
            {
                var created = await _notificationRepository.CreateAsync(notification);
                createdNotifications.Add(created);

                // Process each notification asynchronously
                _ = Task.Run(async () => await ProcessNotificationAsync(created.Id));
            }

            _logger.LogInformation("Created {Count} bulk notifications", createdNotifications.Count);
            return _mapper.Map<IEnumerable<NotificationDto>>(createdNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        try
        {
            var query = "SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId AND c.readAt = null AND c.isDeleted = false";
            var parameters = new { userId };
            
            var result = await _notificationRepository.QueryAsync(query, parameters, userId);
            return result.FirstOrDefault()?.GetHashCode() ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationDto> ScheduleNotificationAsync(ScheduleNotificationDto scheduleNotificationDto)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = scheduleNotificationDto.UserId,
                Title = scheduleNotificationDto.Title,
                Message = scheduleNotificationDto.Message,
                Type = scheduleNotificationDto.Type,
                Channel = scheduleNotificationDto.Channel,
                Status = NotificationStatus.Pending,
                RelatedEntityId = scheduleNotificationDto.RelatedEntityId,
                RelatedEntityType = scheduleNotificationDto.RelatedEntityType,
                Data = scheduleNotificationDto.Data,
                ScheduledAt = scheduleNotificationDto.ScheduledAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification);

            _logger.LogInformation("Scheduled notification {NotificationId} for {ScheduledAt}", 
                createdNotification.Id, scheduleNotificationDto.ScheduledAt);

            return _mapper.Map<NotificationDto>(createdNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for user {UserId}", scheduleNotificationDto.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationTemplateDto>> GetNotificationTemplatesAsync()
    {
        try
        {
            return await _templateService.GetAllTemplatesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification templates");
            throw;
        }
    }

    public async Task ProcessScheduledNotificationsAsync()
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.scheduledAt <= @now AND c.status = @status AND c.isDeleted = false";
            var parameters = new { 
                now = DateTime.UtcNow, 
                status = NotificationStatus.Pending.ToString() 
            };
            
            var scheduledNotifications = await _notificationRepository.QueryAsync(query, parameters, "");

            foreach (var notification in scheduledNotifications)
            {
                _ = Task.Run(async () => await ProcessNotificationAsync(notification.Id));
            }

            _logger.LogInformation("Processing {Count} scheduled notifications", scheduledNotifications.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
            throw;
        }
    }

    public async Task<bool> UpdateNotificationStatusAsync(string id, NotificationStatus status, string? failureReason = null)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id, id);
            if (notification == null)
            {
                return false;
            }

            notification.Status = status;
            notification.FailureReason = failureReason;
            notification.UpdatedAt = DateTime.UtcNow;

            if (status == NotificationStatus.Sent)
            {
                notification.SentAt = DateTime.UtcNow;
            }

            await _notificationRepository.UpdateAsync(notification);

            _logger.LogInformation("Updated notification {NotificationId} status to {Status}", id, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification status for {NotificationId}", id);
            throw;
        }
    }

    private async Task ProcessNotificationAsync(string notificationId)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId, notificationId);
            if (notification == null)
            {
                return;
            }

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            bool success = false;

            switch (notification.Channel)
            {
                case NotificationChannel.Email:
                    success = await SendEmailNotificationAsync(notification);
                    break;
                case NotificationChannel.SMS:
                    success = await SendSmsNotificationAsync(notification);
                    break;
                case NotificationChannel.PushNotification:
                    success = await SendPushNotificationAsync(notification);
                    break;
                case NotificationChannel.InApp:
                    success = await SendInAppNotificationAsync(notification);
                    break;
            }

            if (!success)
            {
                notification.Status = NotificationStatus.Failed;
                notification.RetryCount++;
            }

            await _notificationRepository.UpdateAsync(notification);

            _logger.LogInformation("Processed notification {NotificationId} via {Channel}, success: {Success}", 
                notificationId, notification.Channel, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification {NotificationId}", notificationId);
            await UpdateNotificationStatusAsync(notificationId, NotificationStatus.Failed, ex.Message);
        }
    }

    private async Task<bool> SendEmailNotificationAsync(Notification notification)
    {
        var emailDto = new EmailNotificationDto
        {
            To = notification.UserId, // This should be resolved to actual email
            Subject = notification.Title,
            Body = notification.Message,
            IsHtml = true
        };

        return await _emailService.SendEmailAsync(emailDto);
    }

    private async Task<bool> SendSmsNotificationAsync(Notification notification)
    {
        var smsDto = new SmsNotificationDto
        {
            To = notification.UserId, // This should be resolved to actual phone number
            Message = $"{notification.Title}: {notification.Message}"
        };

        return await _smsService.SendSmsAsync(smsDto);
    }

    private async Task<bool> SendPushNotificationAsync(Notification notification)
    {
        var pushDto = new PushNotificationDto
        {
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Message,
            Data = notification.Data
        };

        return await _pushNotificationService.SendPushNotificationAsync(pushDto);
    }

    private async Task<bool> SendInAppNotificationAsync(Notification notification)
    {
        try
        {
            await _hubContext.Clients.User(notification.UserId).SendAsync("NewNotification", _mapper.Map<NotificationDto>(notification));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending in-app notification to user {UserId}", notification.UserId);
            return false;
        }
    }
}
