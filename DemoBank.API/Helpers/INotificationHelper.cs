using DemoBank.Core.Models;

namespace DemoBank.API.Helpers;

public interface INotificationHelper
{
    Task CreateNotification(Guid userId, string title, string message, NotificationType type);
}