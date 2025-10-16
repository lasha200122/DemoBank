using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DemoBank.API.Services;

public class SettingsService : ISettingsService
{
    private readonly DemoBankContext _context;
    private readonly ILogger<SettingsService> _logger;
    private readonly INotificationHelper _notificationHelper;

    public SettingsService(
        DemoBankContext context,
        ILogger<SettingsService> logger,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _logger = logger;
        _notificationHelper = notificationHelper;
    }

    public async Task<UserSettingsDto> GetUserSettingsAsync(Guid userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if not exist
            settings = await CreateDefaultSettingsAsync(userId);
        }

        return new UserSettingsDto
        {
            PreferredCurrency = settings.PreferredCurrency,
            Language = settings.Language,
            EmailNotifications = settings.EmailNotifications,
            SmsNotifications = settings.SmsNotifications,
            TwoFactorEnabled = settings.TwoFactorEnabled,
            DailyTransferLimit = settings.DailyTransferLimit,
            DailyWithdrawalLimit = settings.DailyWithdrawalLimit
        };
    }

    public async Task<bool> UpdateUserSettingsAsync(Guid userId, UserSettingsDto settingsDto)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
            return false;

        // Validate currency
        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == settingsDto.PreferredCurrency);

        if (currency == null)
            throw new InvalidOperationException($"Currency {settingsDto.PreferredCurrency} is not supported");

        settings.PreferredCurrency = settingsDto.PreferredCurrency;
        settings.Language = settingsDto.Language;
        settings.EmailNotifications = settingsDto.EmailNotifications;
        settings.SmsNotifications = settingsDto.SmsNotifications;
        settings.DailyTransferLimit = settingsDto.DailyTransferLimit;
        settings.DailyWithdrawalLimit = settingsDto.DailyWithdrawalLimit;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Settings Updated",
            "Your account settings have been successfully updated.",
            NotificationType.Info
        );

        return true;
    }

    public async Task<SecuritySettingsDto> GetSecuritySettingsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var loginHistory = await GetRecentLoginHistoryAsync(userId);
        var activeSessions = await GetActiveSessionsCountAsync(userId);

        return new SecuritySettingsDto
        {
            TwoFactorEnabled = user.Settings?.TwoFactorEnabled ?? false,
            LastPasswordChange = user.UpdatedAt ?? user.CreatedAt,
            LoginAlerts = user.Settings?.EmailNotifications ?? true,
            SessionTimeout = 60, // minutes
            RequirePasswordForTransfers = true,
            BiometricEnabled = false, // Would be stored in mobile app
            RecentLoginAttempts = loginHistory,
            ActiveSessions = activeSessions,
            TrustedDevices = await GetTrustedDevicesAsync(userId)
        };
    }

    public async Task<bool> UpdateSecuritySettingsAsync(Guid userId, SecuritySettingsDto settings)
    {
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null)
            return false;

        userSettings.TwoFactorEnabled = settings.TwoFactorEnabled;
        userSettings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Security Settings Updated",
            "Your security settings have been updated. If this wasn't you, contact support immediately.",
            NotificationType.Security
        );

        return true;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var accountCount = await _context.Accounts
            .CountAsync(a => a.UserId == userId);

        var transactionCount = await _context.Transactions
            .Include(t => t.Account)
            .CountAsync(t => t.Account.UserId == userId);

        return new UserProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = null, // Would be stored separately
            MemberSince = user.CreatedAt,
            AccountStatus = user.Status,
            VerificationStatus = "Verified", // Simulated
            AccountsCount = accountCount,
            TransactionsCount = transactionCount,
            PreferredCurrency = user.Settings?.PreferredCurrency ?? "EUR",
            Language = user.Settings?.Language ?? "en",
            TimeZone = "UTC" // Would be stored in settings
        };
    }

    public async Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileDto profileDto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return false;

        // Update basic info
        if (!string.IsNullOrEmpty(profileDto.FirstName))
            user.FirstName = profileDto.FirstName;

        if (!string.IsNullOrEmpty(profileDto.LastName))
            user.LastName = profileDto.LastName;

        user.UpdatedAt = DateTime.UtcNow;

        // Update settings if provided
        if (!string.IsNullOrEmpty(profileDto.PreferredCurrency) ||
            !string.IsNullOrEmpty(profileDto.Language))
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                if (!string.IsNullOrEmpty(profileDto.PreferredCurrency))
                    settings.PreferredCurrency = profileDto.PreferredCurrency;

                if (!string.IsNullOrEmpty(profileDto.Language))
                    settings.Language = profileDto.Language;

                settings.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Profile Updated",
            "Your profile information has been successfully updated.",
            NotificationType.Info
        );

        return true;
    }

    public async Task<bool> UpdateProfilePictureAsync(Guid userId, string pictureUrl)
    {
        // In a real application, this would store the URL in a user profile table
        // and handle image upload/storage

        await _notificationHelper.CreateNotification(
            userId,
            "Profile Picture Updated",
            "Your profile picture has been updated.",
            NotificationType.Info
        );

        return true;
    }

    public async Task<PrivacySettingsDto> GetPrivacySettingsAsync(Guid userId)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return new PrivacySettingsDto
        {
            ShowProfile = true,
            ShowTransactionHistory = false,
            AllowDataAnalytics = true,
            AllowMarketingEmails = false,
            ShareDataWithPartners = false,
            DataRetentionPeriod = 365, // days
            SearchEngineIndexing = false
        };
    }

    public async Task<bool> UpdatePrivacySettingsAsync(Guid userId, PrivacySettingsDto privacy)
    {
        // In a real application, privacy settings would be stored in a separate table

        await _notificationHelper.CreateNotification(
            userId,
            "Privacy Settings Updated",
            "Your privacy preferences have been updated.",
            NotificationType.Info
        );

        return true;
    }

    public async Task<TwoFactorSetupDto> SetupTwoFactorAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return null;

        // Generate secret key
        var secret = GenerateSecretKey();

        // Generate QR code URI
        var qrCodeUri = GenerateQrCodeUri(user.Email, secret);

        return new TwoFactorSetupDto
        {
            SecretKey = secret,
            QrCodeUri = qrCodeUri,
            ManualEntryKey = FormatSecretForManualEntry(secret),
            BackupCodes = GenerateBackupCodes()
        };
    }

    public async Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
            return false;

        // Verify the code (simplified - in real app would verify against TOTP)
        if (verificationCode.Length != 6)
            return false;

        settings.TwoFactorEnabled = true;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Two-Factor Authentication Enabled",
            "Two-factor authentication has been successfully enabled for your account.",
            NotificationType.Security
        );

        return true;
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId, string password)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return false;

        if (user.Settings != null)
        {
            user.Settings.TwoFactorEnabled = false;
            user.Settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _notificationHelper.CreateNotification(
            userId,
            "Two-Factor Authentication Disabled",
            "Two-factor authentication has been disabled. Your account is now less secure.",
            NotificationType.Warning
        );

        return true;
    }

    public async Task<List<string>> GenerateBackupCodesAsync(Guid userId)
    {
        var codes = GenerateBackupCodes();

        // In real app, these would be hashed and stored in database

        await _notificationHelper.CreateNotification(
            userId,
            "Backup Codes Generated",
            "New backup codes have been generated. Store them safely.",
            NotificationType.Security
        );

        return codes;
    }

    public async Task<List<ApiKeyDto>> GetApiKeysAsync(Guid userId)
    {
        // In a real application, API keys would be stored in a separate table
        var mockApiKeys = new List<ApiKeyDto>
        {
            new ApiKeyDto
            {
                Id = Guid.NewGuid(),
                Name = "Production API Key",
                Key = "sk_live_" + GenerateApiKey(),
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                LastUsed = DateTime.UtcNow.AddHours(-3),
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                Permissions = new List<string> { "read", "write", "transfer" }
            }
        };

        return await Task.FromResult(mockApiKeys);
    }

    public async Task<ApiKeyDto> CreateApiKeyAsync(Guid userId, CreateApiKeyDto apiKeyDto)
    {
        var apiKey = new ApiKeyDto
        {
            Id = Guid.NewGuid(),
            Name = apiKeyDto.Name,
            Key = "sk_live_" + GenerateApiKey(),
            CreatedAt = DateTime.UtcNow,
            LastUsed = null,
            ExpiresAt = apiKeyDto.ExpiresIn.HasValue ?
                DateTime.UtcNow.AddDays(apiKeyDto.ExpiresIn.Value) :
                DateTime.UtcNow.AddYears(1),
            Permissions = apiKeyDto.Permissions
        };

        // In real app, store in database

        await _notificationHelper.CreateNotification(
            userId,
            "API Key Created",
            $"New API key '{apiKeyDto.Name}' has been created.",
            NotificationType.Security
        );

        return apiKey;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid userId, Guid apiKeyId)
    {
        // In real app, mark as revoked in database

        await _notificationHelper.CreateNotification(
            userId,
            "API Key Revoked",
            "API key has been revoked and can no longer be used.",
            NotificationType.Security
        );

        return true;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId)
    {
        // In real app, this would query active sessions from cache or database
        var sessions = new List<SessionDto>
        {
            new SessionDto
            {
                Id = Guid.NewGuid(),
                Device = "Chrome on Windows",
                IpAddress = "192.168.1.1",
                Location = "New York, US",
                LastActivity = DateTime.UtcNow,
                LoginTime = DateTime.UtcNow.AddHours(-2),
                IsCurrent = true
            },
            new SessionDto
            {
                Id = Guid.NewGuid(),
                Device = "Mobile App iOS",
                IpAddress = "10.0.0.1",
                Location = "San Francisco, US",
                LastActivity = DateTime.UtcNow.AddMinutes(-30),
                LoginTime = DateTime.UtcNow.AddDays(-1),
                IsCurrent = false
            }
        };

        return await Task.FromResult(sessions);
    }

    public async Task<bool> TerminateSessionAsync(Guid userId, Guid sessionId)
    {
        // In real app, invalidate session token

        await _notificationHelper.CreateNotification(
            userId,
            "Session Terminated",
            "A session has been terminated from your account.",
            NotificationType.Security
        );

        return true;
    }

    public async Task<bool> TerminateAllSessionsAsync(Guid userId)
    {
        // In real app, invalidate all session tokens except current

        await _notificationHelper.CreateNotification(
            userId,
            "All Sessions Terminated",
            "All sessions except the current one have been terminated.",
            NotificationType.Security
        );

        return true;
    }

    public async Task<UserDataExportDto> ExportUserDataAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Settings)
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId)
            .ToListAsync();

        var loans = await _context.Loans
            .Where(l => l.UserId == userId)
            .ToListAsync();

        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var dataExport = new UserDataExportDto
        {
            ExportDate = DateTime.UtcNow,
            UserInfo = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.CreatedAt
            },
            Accounts = user.Accounts.Select(a => new
            {
                a.AccountNumber,
                a.Type,
                a.Currency,
                a.Balance,
                a.CreatedAt
            }),
            TransactionCount = transactions.Count,
            LoanCount = loans.Count,
            InvoiceCount = invoices.Count,
            DataSizeBytes = EstimateDataSize(transactions.Count, loans.Count, invoices.Count)
        };

        await _notificationHelper.CreateNotification(
            userId,
            "Data Export Completed",
            "Your data export has been prepared and is ready for download.",
            NotificationType.Info
        );

        return dataExport;
    }

    public async Task<bool> RequestDataDeletionAsync(Guid userId, string reason)
    {
        // In real app, this would create a deletion request for admin approval

        await _notificationHelper.CreateNotification(
            userId,
            "Data Deletion Request Received",
            "Your request to delete your account data has been received. An administrator will review it within 30 days.",
            NotificationType.Warning
        );

        // Notify admins
        var admins = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.Status == Status.Active)
            .ToListAsync();

        foreach (var admin in admins)
        {
            await _notificationHelper.CreateNotification(
                admin.Id,
                "Data Deletion Request",
                $"User {userId} has requested account deletion. Reason: {reason}",
                NotificationType.Warning
            );
        }

        return true;
    }

    public async Task<SystemSettingsDto> GetSystemSettingsAsync()
    {
        // In real app, these would be stored in a configuration table
        return new SystemSettingsDto
        {
            MaintenanceMode = false,
            MaintenanceMessage = null,
            MaxLoginAttempts = 5,
            SessionTimeout = 60,
            PasswordMinLength = 8,
            RequireSpecialCharacters = true,
            RequireNumbers = true,
            RequireUppercase = true,
            PasswordExpiryDays = 90,
            MinimumAge = 18,
            MaxTransferLimit = 1000000,
            MaxWithdrawalLimit = 500000,
            DefaultCurrency = "EUR",
            SupportedCurrencies = await _context.Currencies
                .Where(c => c.IsActive)
                .Select(c => c.Code)
                .ToListAsync(),
            EmailSettings = new EmailSettingsDto
            {
                SmtpHost = "smtp.example.com",
                SmtpPort = 587,
                FromEmail = "noreply@demobank.com",
                FromName = "DemoBank"
            }
        };
    }

    public async Task<bool> UpdateSystemSettingsAsync(SystemSettingsDto settings)
    {
        // In real app, update configuration table

        _logger.LogInformation("System settings updated");

        if (settings.MaintenanceMode)
        {
            // Notify all users about maintenance
            var activeUsers = await _context.Users
                .Where(u => u.Status == Status.Active)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in activeUsers)
            {
                await _notificationHelper.CreateNotification(
                    userId,
                    "Scheduled Maintenance",
                    settings.MaintenanceMessage ?? "The system will undergo maintenance soon.",
                    NotificationType.Warning
                );
            }
        }

        return true;
    }

    // Helper methods
    private async Task<UserSettings> CreateDefaultSettingsAsync(Guid userId)
    {
        var settings = new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreferredCurrency = "EUR",
            Language = "en",
            EmailNotifications = true,
            SmsNotifications = false,
            TwoFactorEnabled = false,
            DailyTransferLimit = 10000,
            DailyWithdrawalLimit = 5000,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserSettings.Add(settings);
        await _context.SaveChangesAsync();

        return settings;
    }

    private async Task<List<LoginAttemptDto>> GetRecentLoginHistoryAsync(Guid userId)
    {
        // In real app, this would be tracked in a separate table
        return new List<LoginAttemptDto>
        {
            new LoginAttemptDto
            {
                Timestamp = DateTime.UtcNow.AddHours(-1),
                IpAddress = "192.168.1.1",
                Location = "New York, US",
                Device = "Chrome on Windows",
                Success = true
            }
        };
    }

    private async Task<int> GetActiveSessionsCountAsync(Guid userId)
    {
        // In real app, count active sessions
        return await Task.FromResult(2);
    }

    private async Task<List<TrustedDeviceDto>> GetTrustedDevicesAsync(Guid userId)
    {
        // In real app, query trusted devices table
        return new List<TrustedDeviceDto>
        {
            new TrustedDeviceDto
            {
                Id = Guid.NewGuid(),
                DeviceName = "John's iPhone",
                DeviceType = "Mobile",
                LastUsed = DateTime.UtcNow.AddDays(-1),
                AddedOn = DateTime.UtcNow.AddMonths(-1)
            }
        };
    }

    private string GenerateSecretKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    private string GenerateQrCodeUri(string email, string secret)
    {
        var issuer = "DemoBank";
        var accountName = email;
        return $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}";
    }

    private string FormatSecretForManualEntry(string secret)
    {
        // Format secret in groups of 4 for easy manual entry
        var formatted = "";
        for (int i = 0; i < secret.Length; i += 4)
        {
            if (i > 0) formatted += " ";
            formatted += secret.Substring(i, Math.Min(4, secret.Length - i));
        }
        return formatted;
    }

    private List<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        var random = new Random();

        for (int i = 0; i < 10; i++)
        {
            codes.Add($"{random.Next(1000, 9999)}-{random.Next(1000, 9999)}");
        }

        return codes;
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Substring(0, 32);
    }

    private long EstimateDataSize(int transactionCount, int loanCount, int invoiceCount)
    {
        // Rough estimate of data size in bytes
        return (transactionCount * 500) + (loanCount * 1000) + (invoiceCount * 800) + 10000;
    }
}