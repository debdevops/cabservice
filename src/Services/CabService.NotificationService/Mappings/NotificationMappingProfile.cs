using AutoMapper;
using CabService.NotificationService.DTOs;
using CabService.NotificationService.Services;
using CabService.Shared.Models;

namespace CabService.NotificationService.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // Notification mappings
        CreateMap<Notification, NotificationDto>();
        CreateMap<SendNotificationDto, Notification>();
        CreateMap<ScheduleNotificationDto, Notification>();

        // Template mappings
        CreateMap<NotificationTemplate, NotificationTemplateDto>();
        CreateMap<NotificationTemplateDto, NotificationTemplate>();

        // User preferences mappings
        CreateMap<UserNotificationPreferences, UserNotificationPreferencesDto>();
        CreateMap<UserNotificationPreferencesDto, UserNotificationPreferences>();

        // Device token mappings
        CreateMap<DeviceToken, DeviceTokenDto>();
        CreateMap<DeviceTokenDto, DeviceToken>();
    }
}
