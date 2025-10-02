using DemoBank.API.Data;
using DemoBank.Core.Models;

namespace DemoBank.API.Helpers;

public class NotificationHelper : INotificationHelper
{
    private readonly DemoBankContext _context;

    public NotificationHelper(DemoBankContext context)
    {
        _context = context;
    }

    public async Task CreateNotification(Guid userId, string title, string message, NotificationType type)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}