using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface ISettingsService
{
    // User Settings
    Task<UserSettingsDto> GetUserSettingsAsync(Guid userId);
    Task<bool> UpdateUserSettingsAsync(Guid userId, UserSettingsDto settings);
    Task<SecuritySettingsDto> GetSecuritySettingsAsync(Guid userId);
    Task<bool> UpdateSecuritySettingsAsync(Guid userId, SecuritySettingsDto settings);

    // Profile Management
    Task<UserProfileDto> GetUserProfileAsync(Guid userId);
    Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileDto profile);
    Task<bool> UpdateProfilePictureAsync(Guid userId, string pictureUrl);

    // Privacy Settings
    Task<PrivacySettingsDto> GetPrivacySettingsAsync(Guid userId);
    Task<bool> UpdatePrivacySettingsAsync(Guid userId, PrivacySettingsDto privacy);

    // Two-Factor Authentication
    Task<TwoFactorSetupDto> SetupTwoFactorAsync(Guid userId);
    Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(Guid userId, string password);
    Task<List<string>> GenerateBackupCodesAsync(Guid userId);

    // API Keys Management
    Task<List<ApiKeyDto>> GetApiKeysAsync(Guid userId);
    Task<ApiKeyDto> CreateApiKeyAsync(Guid userId, CreateApiKeyDto apiKeyDto);
    Task<bool> RevokeApiKeyAsync(Guid userId, Guid apiKeyId);

    // Session Management
    Task<List<SessionDto>> GetActiveSessionsAsync(Guid userId);
    Task<bool> TerminateSessionAsync(Guid userId, Guid sessionId);
    Task<bool> TerminateAllSessionsAsync(Guid userId);

    // Data Export
    Task<UserDataExportDto> ExportUserDataAsync(Guid userId);
    Task<bool> RequestDataDeletionAsync(Guid userId, string reason);

    // System Settings (Admin only)
    Task<SystemSettingsDto> GetSystemSettingsAsync();
    Task<bool> UpdateSystemSettingsAsync(SystemSettingsDto settings);
}