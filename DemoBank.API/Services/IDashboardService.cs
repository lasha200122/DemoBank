using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IDashboardService
{
    Task<EnhancedDashboardDto> GetEnhancedDashboardAsync(Guid userId);
    Task<AnalyticsDto> GetAnalyticsAsync(Guid userId, int months = 6);
    Task<ActivityFeedDto> GetActivityFeedAsync(Guid userId, int days = 7);
    Task<QuickActionsDto> GetQuickActionsAsync(Guid userId);
    Task<SystemStatusDto> GetSystemStatusAsync();
    Task<FinancialHealthDto> GetFinancialHealthAsync(Guid userId);
    Task<SpendingAnalysisDto> GetSpendingAnalysisAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<GoalsProgressDto> GetGoalsProgressAsync(Guid userId);
}