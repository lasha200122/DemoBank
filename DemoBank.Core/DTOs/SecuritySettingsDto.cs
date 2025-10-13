using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class SecuritySettingsDto
{
    public bool TwoFactorEnabled { get; set; }
    public DateTime LastPasswordChange { get; set; }
    public bool LoginAlerts { get; set; }
    public int SessionTimeout { get; set; } // minutes
    public bool RequirePasswordForTransfers { get; set; }
    public bool BiometricEnabled { get; set; }
    public List<LoginAttemptDto> RecentLoginAttempts { get; set; }
    public int ActiveSessions { get; set; }
    public List<TrustedDeviceDto> TrustedDevices { get; set; }
}

public class LoginAttemptDto
{
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    public string Location { get; set; }
    public string Device { get; set; }
    public bool Success { get; set; }
}

public class TrustedDeviceDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; }
    public string DeviceType { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime AddedOn { get; set; }
}

// Profile Management DTOs
public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ProfilePictureUrl { get; set; }
    public DateTime MemberSince { get; set; }
    public string AccountStatus { get; set; }
    public string VerificationStatus { get; set; }
    public int AccountsCount { get; set; }
    public int TransactionsCount { get; set; }
    public string PreferredCurrency { get; set; }
    public string Language { get; set; }
    public string TimeZone { get; set; }
}

public class UpdateProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PreferredCurrency { get; set; }
    public string Language { get; set; }
    public string TimeZone { get; set; }
}

// Privacy Settings DTOs
public class PrivacySettingsDto
{
    public bool ShowProfile { get; set; }
    public bool ShowTransactionHistory { get; set; }
    public bool AllowDataAnalytics { get; set; }
    public bool AllowMarketingEmails { get; set; }
    public bool ShareDataWithPartners { get; set; }
    public int DataRetentionPeriod { get; set; } // days
    public bool SearchEngineIndexing { get; set; }
}

// Two-Factor Authentication DTOs
public class TwoFactorSetupDto
{
    public string SecretKey { get; set; }
    public string QrCodeUri { get; set; }
    public string ManualEntryKey { get; set; }
    public List<string> BackupCodes { get; set; }
}

public class EnableTwoFactorDto
{
    public string VerificationCode { get; set; }
}

public class DisableTwoFactorDto
{
    public string Password { get; set; }
}

// API Key Management DTOs
public class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Key { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Permissions { get; set; }
}

public class CreateApiKeyDto
{
    public string Name { get; set; }
    public List<string> Permissions { get; set; }
    public int? ExpiresIn { get; set; } // days
}

// Session Management DTOs
public class SessionDto
{
    public Guid Id { get; set; }
    public string Device { get; set; }
    public string IpAddress { get; set; }
    public string Location { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsCurrent { get; set; }
}

// Data Management DTOs
public class UserDataExportDto
{
    public DateTime ExportDate { get; set; }
    public object UserInfo { get; set; }
    public object Accounts { get; set; }
    public int TransactionCount { get; set; }
    public int LoanCount { get; set; }
    public int InvoiceCount { get; set; }
    public long DataSizeBytes { get; set; }
}

public class DataDeletionRequestDto
{
    public string Reason { get; set; }
    public bool ConfirmDeletion { get; set; }
}

// System Settings DTOs (Admin)
public class SystemSettingsDto
{
    public bool MaintenanceMode { get; set; }
    public string MaintenanceMessage { get; set; }
    public int MaxLoginAttempts { get; set; }
    public int SessionTimeout { get; set; } // minutes
    public int PasswordMinLength { get; set; }
    public bool RequireSpecialCharacters { get; set; }
    public bool RequireNumbers { get; set; }
    public bool RequireUppercase { get; set; }
    public int PasswordExpiryDays { get; set; }
    public int MinimumAge { get; set; }
    public decimal MaxTransferLimit { get; set; }
    public decimal MaxWithdrawalLimit { get; set; }
    public string DefaultCurrency { get; set; }
    public List<string> SupportedCurrencies { get; set; }
    public EmailSettingsDto EmailSettings { get; set; }
}

public class EmailSettingsDto
{
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
}

// Audit Log DTOs
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; }
    public string Details { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLogFilterDto
{
    public Guid? UserId { get; set; }
    public string Action { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// Performance Metrics DTOs
public class PerformanceMetricsDto
{
    public double AverageResponseTime { get; set; } // milliseconds
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; } // percentage
    public Dictionary<string, double> EndpointMetrics { get; set; }
    public List<TimeSeriesMetricDto> TimeSeriesData { get; set; }
}

public class TimeSeriesMetricDto
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Metric { get; set; }
}

// Feature Flags DTOs
public class FeatureFlagsDto
{
    public bool EnableCryptoTrading { get; set; }
    public bool EnableInternationalTransfers { get; set; }
    public bool EnableBudgeting { get; set; }
    public bool EnableInvestments { get; set; }
    public bool EnableMobileCheck { get; set; }
    public bool EnableP2PPayments { get; set; }
    public bool EnableCardManagement { get; set; }
    public bool EnableRewards { get; set; }
}

// Compliance Settings DTOs
public class ComplianceSettingsDto
{
    public bool KycRequired { get; set; }
    public string KycLevel { get; set; }
    public bool AmlEnabled { get; set; }
    public decimal SuspiciousTransactionThreshold { get; set; }
    public List<string> RestrictedCountries { get; set; }
    public List<string> RequiredDocuments { get; set; }
    public int DocumentExpiryDays { get; set; }
}

// Rate Limiting DTOs
public class RateLimitSettingsDto
{
    public int MaxRequestsPerMinute { get; set; }
    public int MaxRequestsPerHour { get; set; }
    public int MaxLoginAttemptsPerHour { get; set; }
    public int MaxTransactionsPerDay { get; set; }
    public Dictionary<string, int> EndpointLimits { get; set; }
}

// Backup Settings DTOs
public class BackupSettingsDto
{
    public bool AutoBackupEnabled { get; set; }
    public string BackupFrequency { get; set; } // Daily, Weekly, Monthly
    public TimeSpan BackupTime { get; set; }
    public int RetentionDays { get; set; }
    public string BackupLocation { get; set; }
    public DateTime? LastBackupDate { get; set; }
    public long LastBackupSize { get; set; }
}

// Integration Settings DTOs
public class IntegrationSettingsDto
{
    public List<ThirdPartyIntegrationDto> Integrations { get; set; }
}

public class ThirdPartyIntegrationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Provider { get; set; }
    public bool Enabled { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSync { get; set; }
    public Dictionary<string, string> Settings { get; set; }
}

// Theme Settings DTOs
public class ThemeSettingsDto
{
    public string Theme { get; set; } // Light, Dark, Auto
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string FontFamily { get; set; }
    public string FontSize { get; set; }
    public bool HighContrast { get; set; }
    public bool ReducedMotion { get; set; }
}

// Language Settings DTOs
public class LanguageSettingsDto
{
    public string CurrentLanguage { get; set; }
    public List<SupportedLanguageDto> SupportedLanguages { get; set; }
    public string DateFormat { get; set; }
    public string TimeFormat { get; set; }
    public string NumberFormat { get; set; }
    public string CurrencyFormat { get; set; }
}

public class SupportedLanguageDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string NativeName { get; set; }
    public bool IsRTL { get; set; }
}