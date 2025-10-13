using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class NotificationService : INotificationService
{
    private readonly DemoBankContext _context;
    private readonly ILogger<NotificationService> _logger;

    // In a real application, you would inject a real-time notification service
    // like SignalR hub context here

    public NotificationService(
        DemoBankContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationListDto> GetNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var unreadCount = await query.CountAsync(n => !n.IsRead);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => MapToNotificationItemDto(n))
            .ToListAsync();

        return new NotificationListDto
        {
            Notifications = notifications,
            UnreadCount = unreadCount,
            TotalCount = totalCount
        };
    }

    public async Task<NotificationListDto> GetUnreadNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => MapToNotificationItemDto(n))
            .ToListAsync();

        return new NotificationListDto
        {
            Notifications = notifications,
            UnreadCount = notifications.Count,
            TotalCount = notifications.Count
        };
    }

    public async Task<NotificationItemDto> GetNotificationByIdAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return null;

        return MapToNotificationItemDto(notification);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadNotifications.Any())
            return true;

        var readTime = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = readTime;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CreateNotificationAsync(CreateNotificationDto notificationDto)
    {
        try
        {
            if (notificationDto.UserId.HasValue)
            {
                // Single user notification
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = notificationDto.UserId.Value,
                    Title = notificationDto.Title,
                    Message = notificationDto.Message,
                    Type = Enum.Parse<NotificationType>(notificationDto.Type),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification
                await SendRealTimeNotificationAsync(
                    notificationDto.UserId.Value,
                    MapToNotificationItemDto(notification));
            }
            else
            {
                // Broadcast to all active users
                var activeUsers = await _context.Users
                    .Where(u => u.Status == Status.Active)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in activeUsers)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Title = notificationDto.Title,
                        Message = notificationDto.Message,
                        Type = Enum.Parse<NotificationType>(notificationDto.Type),
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return false;
        }
    }

    public async Task<bool> CreateBulkNotificationsAsync(BulkNotificationDto bulkDto)
    {
        try
        {
            var notifications = bulkDto.UserIds.Select(userId => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = bulkDto.Title,
                Message = bulkDto.Message,
                Type = Enum.Parse<NotificationType>(bulkDto.Type),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Send real-time notifications
            foreach (var notification in notifications)
            {
                await SendRealTimeNotificationAsync(
                    notification.UserId,
                    MapToNotificationItemDto(notification));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk notifications");
            return false;
        }
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
            return new NotificationPreferencesDto
            {
                EmailNotifications = true,
                SmsNotifications = false,
                PushNotifications = true,
                NotificationTypes = GetDefaultNotificationTypes()
            };

        return new NotificationPreferencesDto
        {
            EmailNotifications = settings.EmailNotifications,
            SmsNotifications = settings.SmsNotifications,
            PushNotifications = true, // Default as not stored in current model
            NotificationTypes = GetDefaultNotificationTypes()
        };
    }

    public async Task<bool> UpdatePreferencesAsync(Guid userId, NotificationPreferencesDto preferences)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
            return false;

        settings.EmailNotifications = preferences.EmailNotifications;
        settings.SmsNotifications = preferences.SmsNotifications;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SendRealTimeNotificationAsync(Guid userId, NotificationItemDto notification)
    {
        // In a real application, this would use SignalR or similar
        // to send real-time notifications to connected clients

        _logger.LogInformation($"Sending real-time notification to user {userId}: {notification.Title}");

        // Simulate async operation
        await Task.Delay(10);

        // Here you would typically:
        // 1. Check if user is online
        // 2. Send notification via SignalR hub
        // 3. Queue for push notification if mobile app
        // 4. Queue for email if enabled
    }

    public async Task<Dictionary<string, int>> GetNotificationStatisticsAsync(Guid userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.CreatedAt >= thirtyDaysAgo)
            .ToListAsync();

        var statistics = new Dictionary<string, int>
        {
            { "Total", notifications.Count },
            { "Unread", notifications.Count(n => !n.IsRead) },
            { "Info", notifications.Count(n => n.Type == NotificationType.Info) },
            { "Success", notifications.Count(n => n.Type == NotificationType.Success) },
            { "Warning", notifications.Count(n => n.Type == NotificationType.Warning) },
            { "Error", notifications.Count(n => n.Type == NotificationType.Error) },
            { "Transaction", notifications.Count(n => n.Type == NotificationType.Transaction) },
            { "Security", notifications.Count(n => n.Type == NotificationType.Security) }
        };

        return statistics;
    }

    // Helper methods
    private static NotificationItemDto MapToNotificationItemDto(Notification notification)
    {
        return new NotificationItemDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            Icon = GetNotificationIcon(notification.Type),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            ActionUrl = GetActionUrl(notification)
        };
    }

    private static string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => "info-circle",
            NotificationType.Success => "check-circle",
            NotificationType.Warning => "exclamation-triangle",
            NotificationType.Error => "times-circle",
            NotificationType.Transaction => "exchange-alt",
            NotificationType.Security => "shield-alt",
            _ => "bell"
        };
    }

    private static string GetActionUrl(Notification notification)
    {
        // Parse notification message to determine relevant action URL
        if (notification.Message.Contains("invoice", StringComparison.OrdinalIgnoreCase))
            return "/invoices";
        if (notification.Message.Contains("loan", StringComparison.OrdinalIgnoreCase))
            return "/loans";
        if (notification.Message.Contains("transfer", StringComparison.OrdinalIgnoreCase))
            return "/transfers";
        if (notification.Message.Contains("account", StringComparison.OrdinalIgnoreCase))
            return "/accounts";

        return null;
    }

    private static Dictionary<string, bool> GetDefaultNotificationTypes()
    {
        return new Dictionary<string, bool>
        {
            { "Transactions", true },
            { "Security", true },
            { "Account", true },
            { "Marketing", false },
            { "System", true },
            { "Loans", true },
            { "Invoices", true }
        };
    }
}

// Real-time Notification Hub (for SignalR implementation)
public interface INotificationHub
{
    Task SendNotification(string userId, NotificationItemDto notification);
    Task SendBulkNotification(List<string> userIds, NotificationItemDto notification);
    Task UpdateNotificationCount(string userId, int unreadCount);
}

// Background service for scheduled notifications
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DemoBankContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Check for upcoming loan payments
                await CheckUpcomingLoanPayments(context, notificationService);

                // Check for overdue invoices
                await CheckOverdueInvoices(context, notificationService);

                // Check for low balance accounts
                await CheckLowBalanceAccounts(context, notificationService);

                // Wait for next check (every hour)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckUpcomingLoanPayments(DemoBankContext context, INotificationService notificationService)
    {
        var upcomingPayments = await context.Loans
            .Where(l => l.Status == LoanStatus.Active &&
                       l.NextPaymentDate.HasValue &&
                       l.NextPaymentDate.Value <= DateTime.UtcNow.AddDays(3))
            .ToListAsync();

        foreach (var loan in upcomingPayments)
        {
            var notification = new CreateNotificationDto
            {
                UserId = loan.UserId,
                Title = "Upcoming Loan Payment",
                Message = $"Your loan payment of ${loan.MonthlyPayment:N2} is due on {loan.NextPaymentDate:yyyy-MM-dd}",
                Type = NotificationType.Info.ToString()
            };

            await notificationService.CreateNotificationAsync(notification);
        }
    }

    private async Task CheckOverdueInvoices(DemoBankContext context, INotificationService notificationService)
    {
        var overdueInvoices = await context.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent &&
                       i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;

            var notification = new CreateNotificationDto
            {
                UserId = invoice.UserId,
                Title = "Invoice Overdue",
                Message = $"Invoice #{invoice.InvoiceNumber} for {invoice.Currency} {invoice.Amount:N2} is overdue",
                Type = NotificationType.Warning.ToString()
            };

            await notificationService.CreateNotificationAsync(notification);
        }

        if (overdueInvoices.Any())
            await context.SaveChangesAsync();
    }

    private async Task CheckLowBalanceAccounts(DemoBankContext context, INotificationService notificationService)
    {
        var lowBalanceAccounts = await context.Accounts
            .Include(a => a.User)
            .Where(a => a.IsActive && a.Balance < 100 && a.Balance >= 0)
            .ToListAsync();

        var userNotifications = lowBalanceAccounts
            .GroupBy(a => a.UserId)
            .Select(g => new CreateNotificationDto
            {
                UserId = g.Key,
                Title = "Low Balance Alert",
                Message = $"You have {g.Count()} account(s) with low balance",
                Type = NotificationType.Warning.ToString()
            });

        foreach (var notification in userNotifications)
        {
            await notificationService.CreateNotificationAsync(notification);
        }
    }
}