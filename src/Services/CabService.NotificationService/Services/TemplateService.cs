using AutoMapper;
using CabService.NotificationService.DTOs;
using CabService.Shared.Infrastructure;
using CabService.Shared.Models;
using System.Text.RegularExpressions;

namespace CabService.NotificationService.Services;

public class TemplateService : ITemplateService
{
    private readonly ICosmosRepository<NotificationTemplate> _templateRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ICosmosRepository<NotificationTemplate> templateRepository,
        IMapper mapper,
        ILogger<TemplateService> logger)
    {
        _templateRepository = templateRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return Task.FromResult(string.Empty);
            }

            var processedTemplate = template;

            // Replace variables in the format {{variableName}}
            foreach (var variable in variables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                var value = variable.Value?.ToString() ?? string.Empty;
                processedTemplate = processedTemplate.Replace(placeholder, value);
            }

            return Task.FromResult(processedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template");
            return Task.FromResult(template);
        }
    }

    public async Task<NotificationTemplateDto?> GetTemplateAsync(NotificationType type, NotificationChannel channel)
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.type = @type AND c.channel = @channel AND c.isActive = true AND c.isDeleted = false";
            var parameters = new { 
                type = type.ToString(), 
                channel = channel.ToString() 
            };
            
            var templates = await _templateRepository.QueryAsync(query, parameters, type.ToString());
            var template = templates.FirstOrDefault();

            return template != null ? _mapper.Map<NotificationTemplateDto>(template) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template for type {Type} and channel {Channel}", type, channel);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationTemplateDto>> GetAllTemplatesAsync()
    {
        try
        {
            var query = "SELECT * FROM c WHERE c.isDeleted = false ORDER BY c.type, c.channel";
            var templates = await _templateRepository.QueryAsync(query, null, string.Empty);
            return _mapper.Map<IEnumerable<NotificationTemplateDto>>(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all templates");
            throw;
        }
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(NotificationTemplateDto templateDto)
    {
        try
        {
            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = templateDto.Name,
                Type = templateDto.Type,
                Channel = templateDto.Channel,
                Subject = templateDto.Subject,
                Body = templateDto.Body,
                Variables = templateDto.Variables,
                IsActive = templateDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdTemplate = await _templateRepository.CreateAsync(template);
            _logger.LogInformation("Created notification template {TemplateId}", createdTemplate.Id);

            return _mapper.Map<NotificationTemplateDto>(createdTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            throw;
        }
    }

    public async Task<NotificationTemplateDto?> UpdateTemplateAsync(string id, NotificationTemplateDto templateDto)
    {
        try
        {
            var existingTemplate = await _templateRepository.GetByIdAsync(id, id);
            if (existingTemplate == null)
            {
                return null;
            }

            existingTemplate.Name = templateDto.Name;
            existingTemplate.Subject = templateDto.Subject;
            existingTemplate.Body = templateDto.Body;
            existingTemplate.Variables = templateDto.Variables;
            existingTemplate.IsActive = templateDto.IsActive;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            var updatedTemplate = await _templateRepository.UpdateAsync(existingTemplate);
            _logger.LogInformation("Updated notification template {TemplateId}", id);

            return _mapper.Map<NotificationTemplateDto>(updatedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(string id)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(id, id);
            if (template == null)
            {
                return false;
            }

            await _templateRepository.DeleteAsync(id, id);
            _logger.LogInformation("Deleted notification template {TemplateId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            throw;
        }
    }
}

// NotificationTemplate model
public class NotificationTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Variables { get; set; }
    public bool IsActive { get; set; } = true;
}
