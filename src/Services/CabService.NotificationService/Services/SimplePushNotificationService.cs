using CabService.NotificationService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;

namespace CabService.NotificationService.Services;

public class SimplePushNotificationService : IPushNotificationService
{
    private readonly ICosmosRepository<DeviceToken> _deviceTokenRepository;
    private readonly ILogger<SimplePushNotificationService> _logger;

    public SimplePushNotificationService(
        ICosmosRepository<DeviceToken> deviceTokenRepository,
        ILogger<SimplePushNotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _logger = logger;
    }

    public async Task<bool> SendPushNotificationAsync(PushNotificationDto pushDto)
    {
        try
        {
            // Simplified implementation - log the notification
            _logger.LogInformation("Sending push notification to user {UserId}: {Title} - {Body}", 
                pushDto.UserId, pushDto.Title, pushDto.Body);
            
            // In a real implementation, this would integrate with a push notification service
            // For now, we'll simulate success
            await Task.Delay(100); // Simulate async operation
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", pushDto.UserId);
            return false;
        }
    }

    public async Task<bool> SendBulkPushNotificationAsync(IEnumerable<PushNotificationDto> pushDtos)
    {
        try
        {
            var tasks = pushDtos.Select(SendPushNotificationAsync);
            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r);
            var totalCount = results.Length;

            _logger.LogInformation("Bulk push notification send completed: {SuccessCount}/{TotalCount} successful", 
                successCount, totalCount);

            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk push notifications");
            return false;
        }
    }

    public async Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform)
    {
        try
        {
            var newToken = new DeviceToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Token = deviceToken,
                Platform = platform,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _deviceTokenRepository.CreateAsync(newToken);
            _logger.LogInformation("Device token registered for user {UserId} on platform {Platform}", userId, platform);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceTokenAsync(string userId, string deviceToken)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.userId = @userId AND c.token = @token AND c.isDeleted = false";
            var parameters = new { userId, token = deviceToken };
            var tokens = await _deviceTokenRepository.QueryAsync(query, parameters, userId);
            
            foreach (var token in tokens)
            {
                token.IsActive = false;
                token.UpdatedAt = DateTime.UtcNow;
                await _deviceTokenRepository.UpdateAsync(token);
            }

            _logger.LogInformation("Device token unregistered for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token for user {UserId}", userId);
            return false;
        }
    }
}
