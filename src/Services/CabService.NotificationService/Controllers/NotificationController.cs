using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabService.NotificationService.Services;
using CabService.NotificationService.DTOs;
using CabService.Shared.Models;

namespace CabService.NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> SendNotification([FromBody] SendNotificationDto sendNotificationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notification = await _notificationService.SendNotificationAsync(sendNotificationDto);
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", sendNotificationDto.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDto>> GetNotification(string id)
    {
        try
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification with ID {NotificationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        try
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(string id)
    {
        try
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<ActionResult> MarkAllAsRead(string userId)
    {
        try
        {
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = $"Marked {count} notifications as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(string id)
    {
        try
        {
            var success = await _notificationService.DeleteNotificationAsync(id);
            if (!success)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification with ID {NotificationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> SendBulkNotifications([FromBody] SendBulkNotificationDto bulkNotificationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notifications = await _notificationService.SendBulkNotificationsAsync(bulkNotificationDto);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("user/{userId}/unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(string userId)
    {
        try
        {
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("schedule")]
    public async Task<ActionResult<NotificationDto>> ScheduleNotification([FromBody] ScheduleNotificationDto scheduleNotificationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notification = await _notificationService.ScheduleNotificationAsync(scheduleNotificationDto);
            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for user {UserId}", scheduleNotificationDto.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<NotificationTemplateDto>>> GetNotificationTemplates()
    {
        try
        {
            var templates = await _notificationService.GetNotificationTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification templates");
            return StatusCode(500, "Internal server error");
        }
    }
}
