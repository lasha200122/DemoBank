using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface INotificationService
{
    Task<NotificationListDto> GetNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<NotificationListDto> GetUnreadNotificationsAsync(Guid userId);
    Task<NotificationItemDto> GetNotificationByIdAsync(Guid notificationId, Guid userId);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<bool> MarkAllAsReadAsync(Guid userId);
    Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task<bool> CreateNotificationAsync(CreateNotificationDto notificationDto);
    Task<bool> CreateBulkNotificationsAsync(BulkNotificationDto bulkDto);
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId);
    Task<bool> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences);
    Task SendRealTimeNotificationAsync(Guid userId, NotificationItemDto notification);
    Task<Dictionary<string, int>> GetNotificationStatisticsAsync(Guid userId);
}