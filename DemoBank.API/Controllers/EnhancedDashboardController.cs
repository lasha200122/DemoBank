using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EnhancedDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<EnhancedDashboardController> _logger;

    public EnhancedDashboardController(
        IDashboardService dashboardService,
        ILogger<EnhancedDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    // GET: api/EnhancedDashboard
    [HttpGet]
    public async Task<IActionResult> GetEnhancedDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dashboard = await _dashboardService.GetEnhancedDashboardAsync(userId);

            return Ok(ResponseDto<EnhancedDashboardDto>.SuccessResponse(dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading enhanced dashboard");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading the dashboard"
            ));
        }
    }

    // GET: api/EnhancedDashboard/analytics
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] int months = 6)
    {
        try
        {
            var userId = GetCurrentUserId();
            var analytics = await _dashboardService.GetAnalyticsAsync(userId, months);

            return Ok(ResponseDto<AnalyticsDto>.SuccessResponse(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading analytics"
            ));
        }
    }

    // GET: api/EnhancedDashboard/activity-feed
    [HttpGet("activity-feed")]
    public async Task<IActionResult> GetActivityFeed([FromQuery] int days = 7)
    {
        try
        {
            var userId = GetCurrentUserId();
            var feed = await _dashboardService.GetActivityFeedAsync(userId, days);

            return Ok(ResponseDto<ActivityFeedDto>.SuccessResponse(feed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity feed");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading activity feed"
            ));
        }
    }

    // GET: api/EnhancedDashboard/quick-actions
    [HttpGet("quick-actions")]
    public async Task<IActionResult> GetQuickActions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var actions = await _dashboardService.GetQuickActionsAsync(userId);

            return Ok(ResponseDto<QuickActionsDto>.SuccessResponse(actions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quick actions");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading quick actions"
            ));
        }
    }

    // GET: api/EnhancedDashboard/financial-health
    [HttpGet("financial-health")]
    public async Task<IActionResult> GetFinancialHealth()
    {
        try
        {
            var userId = GetCurrentUserId();
            var health = await _dashboardService.GetFinancialHealthAsync(userId);

            return Ok(ResponseDto<FinancialHealthDto>.SuccessResponse(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading financial health");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading financial health"
            ));
        }
    }

    // GET: api/EnhancedDashboard/spending-analysis
    [HttpGet("spending-analysis")]
    public async Task<IActionResult> GetSpendingAnalysis(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var analysis = await _dashboardService.GetSpendingAnalysisAsync(userId, start, end);

            return Ok(ResponseDto<SpendingAnalysisDto>.SuccessResponse(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading spending analysis");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading spending analysis"
            ));
        }
    }

    // GET: api/EnhancedDashboard/goals
    [HttpGet("goals")]
    public async Task<IActionResult> GetGoalsProgress()
    {
        try
        {
            var userId = GetCurrentUserId();
            var goals = await _dashboardService.GetGoalsProgressAsync(userId);

            return Ok(ResponseDto<GoalsProgressDto>.SuccessResponse(goals));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading goals progress");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading goals"
            ));
        }
    }

    // GET: api/EnhancedDashboard/system-status
    [HttpGet("system-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSystemStatus()
    {
        try
        {
            var status = await _dashboardService.GetSystemStatusAsync();

            return Ok(ResponseDto<SystemStatusDto>.SuccessResponse(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system status");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading system status"
            ));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }
}

// Notification Controller
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // GET: api/Notification
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);

            return Ok(ResponseDto<NotificationListDto>.SuccessResponse(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching notifications"
            ));
        }
    }

    // GET: api/Notification/unread
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

            return Ok(ResponseDto<NotificationListDto>.SuccessResponse(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unread notifications");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching unread notifications"
            ));
        }
    }

    // PUT: api/Notification/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAsReadAsync(id, userId);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse("Notification not found"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Notification marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating notification"
            ));
        }
    }

    // PUT: api/Notification/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);

            return Ok(ResponseDto<object>.SuccessResponse(null, "All notifications marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating notifications"
            ));
        }
    }

    // DELETE: api/Notification/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.DeleteNotificationAsync(id, userId);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse("Notification not found"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Notification deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while deleting notification"
            ));
        }
    }

    // GET: api/Notification/preferences
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            var preferences = await _notificationService.GetPreferencesAsync(userId);

            return Ok(ResponseDto<NotificationPreferencesDto>.SuccessResponse(preferences));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notification preferences");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching preferences"
            ));
        }
    }

    // PUT: api/Notification/preferences
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferencesDto preferences)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.UpdatePreferencesAsync(userId, preferences);

            return Ok(ResponseDto<object>.SuccessResponse(null, "Preferences updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating preferences"
            ));
        }
    }

    // GET: api/Notification/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var stats = await _notificationService.GetNotificationStatisticsAsync(userId);

            return Ok(ResponseDto<Dictionary<string, int>>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notification statistics");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching statistics"
            ));
        }
    }

    // POST: api/Notification/broadcast (Admin only)
    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BroadcastNotification([FromBody] CreateNotificationDto notification)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(notification);

            return Ok(ResponseDto<object>.SuccessResponse(null, "Notification broadcast sent"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while broadcasting notification"
            ));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }
}

// Settings Controller
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    // GET: api/Settings
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            return Ok(ResponseDto<UserSettingsDto>.SuccessResponse(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching settings"
            ));
        }
    }

    // PUT: api/Settings
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsDto settings)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _settingsService.UpdateUserSettingsAsync(userId, settings);

            return Ok(ResponseDto<object>.SuccessResponse(null, "Settings updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating settings"
            ));
        }
    }

    // GET: api/Settings/security
    [HttpGet("security")]
    public async Task<IActionResult> GetSecuritySettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _settingsService.GetSecuritySettingsAsync(userId);

            return Ok(ResponseDto<SecuritySettingsDto>.SuccessResponse(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching security settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching security settings"
            ));
        }
    }

    // GET: api/Settings/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _settingsService.GetUserProfileAsync(userId);

            return Ok(ResponseDto<UserProfileDto>.SuccessResponse(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching profile"
            ));
        }
    }

    // PUT: api/Settings/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profile)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _settingsService.UpdateUserProfileAsync(userId, profile);

            return Ok(ResponseDto<object>.SuccessResponse(null, "Profile updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating profile"
            ));
        }
    }

    // GET: api/Settings/privacy
    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacySettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var privacy = await _settingsService.GetPrivacySettingsAsync(userId);

            return Ok(ResponseDto<PrivacySettingsDto>.SuccessResponse(privacy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching privacy settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching privacy settings"
            ));
        }
    }

    // POST: api/Settings/2fa/setup
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        try
        {
            var userId = GetCurrentUserId();
            var setup = await _settingsService.SetupTwoFactorAsync(userId);

            return Ok(ResponseDto<TwoFactorSetupDto>.SuccessResponse(setup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up 2FA");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while setting up 2FA"
            ));
        }
    }

    // POST: api/Settings/2fa/enable
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _settingsService.EnableTwoFactorAsync(userId, dto.VerificationCode);

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Invalid verification code"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "2FA enabled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while enabling 2FA"
            ));
        }
    }

    // GET: api/Settings/api-keys
    [HttpGet("api-keys")]
    public async Task<IActionResult> GetApiKeys()
    {
        try
        {
            var userId = GetCurrentUserId();
            var keys = await _settingsService.GetApiKeysAsync(userId);

            return Ok(ResponseDto<List<ApiKeyDto>>.SuccessResponse(keys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API keys");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching API keys"
            ));
        }
    }

    // GET: api/Settings/sessions
    [HttpGet("sessions")]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _settingsService.GetActiveSessionsAsync(userId);

            return Ok(ResponseDto<List<SessionDto>>.SuccessResponse(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sessions");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching sessions"
            ));
        }
    }

    // POST: api/Settings/export-data
    [HttpPost("export-data")]
    public async Task<IActionResult> ExportData()
    {
        try
        {
            var userId = GetCurrentUserId();
            var export = await _settingsService.ExportUserDataAsync(userId);

            return Ok(ResponseDto<UserDataExportDto>.SuccessResponse(export));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while exporting data"
            ));
        }
    }

    // GET: api/Settings/system (Admin only)
    [HttpGet("system")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSystemSettings()
    {
        try
        {
            var settings = await _settingsService.GetSystemSettingsAsync();

            return Ok(ResponseDto<SystemSettingsDto>.SuccessResponse(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching system settings"
            ));
        }
    }

    // PUT: api/Settings/system (Admin only)
    [HttpPut("system")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSystemSettings([FromBody] SystemSettingsDto settings)
    {
        try
        {
            await _settingsService.UpdateSystemSettingsAsync(settings);

            return Ok(ResponseDto<object>.SuccessResponse(null, "System settings updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating system settings"
            ));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }
}