using CabService.NotificationService.DTOs;
using CabService.Shared.Models;

namespace CabService.NotificationService.Services;

public interface ITemplateService
{
    Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables);
    Task<NotificationTemplateDto?> GetTemplateAsync(NotificationType type, NotificationChannel channel);
    Task<IEnumerable<NotificationTemplateDto>> GetAllTemplatesAsync();
    Task<NotificationTemplateDto> CreateTemplateAsync(NotificationTemplateDto templateDto);
    Task<NotificationTemplateDto?> UpdateTemplateAsync(string id, NotificationTemplateDto templateDto);
    Task<bool> DeleteTemplateAsync(string id);
}
