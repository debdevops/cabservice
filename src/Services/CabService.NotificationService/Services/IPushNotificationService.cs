using CabService.NotificationService.DTOs;

namespace CabService.NotificationService.Services;

public interface IPushNotificationService
{
    Task<bool> SendPushNotificationAsync(PushNotificationDto pushDto);
    Task<bool> SendBulkPushNotificationAsync(IEnumerable<PushNotificationDto> pushDtos);
    Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform);
    Task<bool> UnregisterDeviceTokenAsync(string userId, string deviceToken);
}
