using CabService.NotificationService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace CabService.NotificationService.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ICosmosRepository<DeviceToken> _deviceTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly FirebaseMessaging _firebaseMessaging;

    public PushNotificationService(
        ICosmosRepository<DeviceToken> deviceTokenRepository,
        IConfiguration configuration,
        ILogger<PushNotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _configuration = configuration;
        _logger = logger;

        // Initialize Firebase Admin SDK
        var firebaseConfigPath = _configuration["Firebase:ServiceAccountKeyPath"];
        if (!string.IsNullOrEmpty(firebaseConfigPath) && File.Exists(firebaseConfigPath))
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(firebaseConfigPath)
            });
        }
        else
        {
            // Use environment variable or default credentials
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault()
            });
        }

        _firebaseMessaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<bool> SendPushNotificationAsync(PushNotificationDto pushDto)
    {
        try
        {
            // Get user's device tokens
            var deviceTokens = await GetUserDeviceTokensAsync(pushDto.UserId);
            if (!deviceTokens.Any())
            {
                _logger.LogWarning("No device tokens found for user {UserId}", pushDto.UserId);
                return false;
            }

            var message = new MulticastMessage()
            {
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = pushDto.Title,
                    Body = pushDto.Body,
                    ImageUrl = pushDto.ImageUrl
                },
                Data = pushDto.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty),
                Tokens = deviceTokens.Select(dt => dt.Token).ToList()
            };

            // Add platform-specific configuration
            message.Android = new AndroidConfig()
            {
                Priority = Priority.High,
                Notification = new AndroidNotification()
                {
                    Title = pushDto.Title,
                    Body = pushDto.Body,
                    Icon = "ic_notification",
                    Color = "#FF6B35",
                    Sound = "default"
                }
            };

            message.Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Alert = new ApsAlert()
                    {
                        Title = pushDto.Title,
                        Body = pushDto.Body
                    },
                    Badge = pushDto.Badge,
                    Sound = "default"
                }
            };

            var response = await _firebaseMessaging.SendMulticastAsync(message);

            // Handle failed tokens
            if (response.FailureCount > 0)
            {
                var failedTokens = new List<string>();
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        failedTokens.Add(deviceTokens.ElementAt(i).Token);
                        _logger.LogWarning("Failed to send push notification to token {Token}: {Error}", 
                            deviceTokens.ElementAt(i).Token, response.Responses[i].Exception?.Message);
                    }
                }

                // Remove invalid tokens
                await RemoveInvalidTokensAsync(failedTokens);
            }

            _logger.LogInformation("Push notification sent to user {UserId}. Success: {SuccessCount}, Failed: {FailureCount}", 
                pushDto.UserId, response.SuccessCount, response.FailureCount);

            return response.SuccessCount > 0;
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
            // Check if token already exists
            var existingToken = await GetDeviceTokenAsync(userId, deviceToken);
            if (existingToken != null)
            {
                // Update existing token
                existingToken.Platform = platform;
                existingToken.UpdatedAt = DateTime.UtcNow;
                existingToken.IsActive = true;
                await _deviceTokenRepository.UpdateAsync(existingToken);
            }
            else
            {
                // Create new token
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
            }

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
            var token = await GetDeviceTokenAsync(userId, deviceToken);
            if (token != null)
            {
                token.IsActive = false;
                token.UpdatedAt = DateTime.UtcNow;
                await _deviceTokenRepository.UpdateAsync(token);

                _logger.LogInformation("Device token unregistered for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token for user {UserId}", userId);
            return false;
        }
    }

    private async Task<IEnumerable<DeviceToken>> GetUserDeviceTokensAsync(string userId)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.isActive = true AND c.isDeleted = false";
        var parameters = new { userId };
        return await _deviceTokenRepository.QueryAsync(query, parameters, userId);
    }

    private async Task<DeviceToken?> GetDeviceTokenAsync(string userId, string token)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.token = @token AND c.isDeleted = false";
        var parameters = new { userId, token };
        var tokens = await _deviceTokenRepository.QueryAsync(query, parameters, userId);
        return tokens.FirstOrDefault();
    }

    private async Task RemoveInvalidTokensAsync(IEnumerable<string> invalidTokens)
    {
        try
        {
            foreach (var token in invalidTokens)
            {
                var query = "SELECT * FROM c WHERE c.token = @token AND c.isDeleted = false";
                var parameters = new { token };
                var deviceTokens = await _deviceTokenRepository.QueryAsync(query, parameters, "");
                
                foreach (var deviceToken in deviceTokens)
                {
                    deviceToken.IsActive = false;
                    deviceToken.UpdatedAt = DateTime.UtcNow;
                    await _deviceTokenRepository.UpdateAsync(deviceToken);
                }
            }

            _logger.LogInformation("Removed {Count} invalid device tokens", invalidTokens.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing invalid device tokens");
        }
    }
}

// DeviceToken model for storing device registration tokens
public class DeviceToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // iOS, Android, Web
    public bool IsActive { get; set; } = true;
}
