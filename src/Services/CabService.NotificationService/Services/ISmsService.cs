using CabService.NotificationService.DTOs;

namespace CabService.NotificationService.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(SmsNotificationDto smsDto);
    Task<bool> SendBulkSmsAsync(IEnumerable<SmsNotificationDto> smsDtos);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
}
