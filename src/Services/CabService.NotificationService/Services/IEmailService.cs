using CabService.NotificationService.DTOs;

namespace CabService.NotificationService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailNotificationDto emailDto);
    Task<bool> SendBulkEmailAsync(IEnumerable<EmailNotificationDto> emailDtos);
    Task<bool> ValidateEmailAddressAsync(string email);
}
