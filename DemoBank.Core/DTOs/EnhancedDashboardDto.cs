using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class EnhancedDashboardDto
{
    public UserInfoDto UserInfo { get; set; }
    public EnhancedAccountSummaryDto AccountsSummary { get; set; }
    public TransactionMetricsDto TransactionMetrics { get; set; }
    public PendingItemsDto PendingItems { get; set; }
    public Dictionary<string, decimal> SpendingByCategory { get; set; }
    public List<ActivityItemDto> RecentActivity { get; set; }
    public int UnreadNotifications { get; set; }
    public int FinancialScore { get; set; }
    public List<string> Insights { get; set; }
}

public class UserInfoDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime LastLogin { get; set; }
    public string PreferredCurrency { get; set; }
}

public class EnhancedAccountSummaryDto
{
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public decimal TotalBalanceUSD { get; set; }
    public Dictionary<string, decimal> BalancesByCurrency { get; set; }
    public decimal BalanceTrend { get; set; } // Percentage change
    public string PrimaryAccount { get; set; }
}

public class TransactionMetricsDto
{
    public PeriodMetricsDto Last30Days { get; set; }
    public PeriodMetricsDto Last90Days { get; set; }
    public PeriodMetricsDto YearToDate { get; set; }
}

public class PeriodMetricsDto
{
    public int TotalTransactions { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal AverageTransactionSize { get; set; }
}

public class PendingItemsDto
{
    public int PendingInvoices { get; set; }
    public decimal PendingInvoicesAmount { get; set; }
    public int ActiveLoans { get; set; }
    public DateTime? NextLoanPayment { get; set; }
    public decimal TotalLoanBalance { get; set; }
}

public class ActivityItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
}

// Analytics DTOs
public class AnalyticsDto
{
    public string Period { get; set; }
    public List<MonthlyAnalyticsDto> MonthlyData { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal AverageMonthlyIncome { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
    public decimal SavingsRate { get; set; }
    public decimal IncomeTrend { get; set; }
    public decimal ExpenseTrend { get; set; }
}

public class MonthlyAnalyticsDto
{
    public string Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public int TransactionCount { get; set; }
}

// Activity Feed DTOs
public class ActivityFeedDto
{
    public string Period { get; set; }
    public int TotalActivities { get; set; }
    public List<DailyActivityDto> DailyActivities { get; set; }
}

public class DailyActivityDto
{
    public DateTime Date { get; set; }
    public List<ActivityFeedItemDto> Activities { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ActivityFeedItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Category { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; }
    public string Icon { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
}

// Quick Actions DTOs
public class QuickActionsDto
{
    public List<QuickActionDto> AvailableActions { get; set; }
    public List<FrequentRecipientDto> FrequentRecipients { get; set; }
    public List<FavoriteCurrencyPairSimpleDto> FavoriteCurrencyPairs { get; set; }
    public List<string> SuggestedActions { get; set; }
}

public class QuickActionDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public bool Enabled { get; set; }
}

public class FrequentRecipientDto
{
    public string AccountNumber { get; set; }
    public string Name { get; set; }
    public int TransferCount { get; set; }
    public DateTime LastTransfer { get; set; }
}

public class FavoriteCurrencyPairSimpleDto
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal CurrentRate { get; set; }
}

// System Status DTOs
public class SystemStatusDto
{
    public string Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<ServiceStatusDto> Services { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int TransactionsToday { get; set; }
    public decimal SystemLoad { get; set; }
}

public class ServiceStatusDto
{
    public string Name { get; set; }
    public string Status { get; set; }
    public int ResponseTime { get; set; } // milliseconds
    public decimal Uptime { get; set; } // percentage
}

// Financial Health DTOs
public class FinancialHealthDto
{
    public int HealthScore { get; set; }
    public string ScoreCategory { get; set; }
    public HealthMetricsDto Metrics { get; set; }
    public List<string> Recommendations { get; set; }
    public HealthTrendsDto Trends { get; set; }
}

public class HealthMetricsDto
{
    public decimal SavingsRate { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal EmergencyFundMonths { get; set; }
    public decimal CreditUtilization { get; set; }
    public decimal NetWorth { get; set; }
    public decimal MonthlyNetIncome { get; set; }
}

public class HealthTrendsDto
{
    public decimal ScoreTrend { get; set; }
    public decimal SavingsTrend { get; set; }
    public decimal DebtTrend { get; set; }
}

// Spending Analysis DTOs
public class SpendingAnalysisDto
{
    public string Period { get; set; }
    public decimal TotalSpending { get; set; }
    public List<CategorySpendingDto> Categories { get; set; }
    public List<MerchantSpendingDto> TopMerchants { get; set; }
    public decimal DailyAverage { get; set; }
    public DateTime? HighestSpendingDay { get; set; }
}

public class CategorySpendingDto
{
    public string Category { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int TransactionCount { get; set; }
}

public class MerchantSpendingDto
{
    public string Merchant { get; set; }
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

// Goals DTOs
public class GoalsProgressDto
{
    public List<GoalDto> Goals { get; set; }
    public int TotalGoals { get; set; }
    public int CompletedGoals { get; set; }
    public decimal AverageProgress { get; set; }
}

public class GoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Progress { get; set; } // Percentage
    public DateTime TargetDate { get; set; }
    public string Category { get; set; }
    public string Status { get; set; }
    public decimal MonthlyContribution { get; set; }
}

// Notification DTOs
public class NotificationListDto
{
    public List<NotificationItemDto> Notifications { get; set; }
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
}

public class NotificationItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public string Icon { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string ActionUrl { get; set; }
}

public class NotificationPreferencesDto
{
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; }
    public Dictionary<string, bool> NotificationTypes { get; set; }
}

public class CreateNotificationDto
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public string ActionUrl { get; set; }
    public Guid? UserId { get; set; } // Optional, for broadcast notifications
}

public class BulkNotificationDto
{
    public List<Guid> UserIds { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}