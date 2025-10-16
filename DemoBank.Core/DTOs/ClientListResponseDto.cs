    using System.ComponentModel.DataAnnotations;

    namespace DemoBank.Core.DTOs;

    public class ClientListResponseDto
    {
        public List<ClientSummaryDto> Clients { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ClientSummaryDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Quick stats
        public int AccountCount { get; set; }
        public decimal TotalBalanceEUR { get; set; }
        public int ActiveLoans { get; set; }
        public int ActiveInvestments { get; set; }
    }

    // Detailed Client Information
    public class ClientDetailsDto
    {
        // Personal Information
        public PersonalInfoDto PersonalInfo { get; set; }

        // Account Summary
        public ClientAccountSummaryDto AccountSummary { get; set; }

        // Financial Summary
        public ClientFinancialSummaryDto FinancialSummary { get; set; }

        // Activity Summary
        public ClientActivitySummaryDto ActivitySummary { get; set; }

        // Risk Profile
        public ClientRiskProfileDto RiskProfile { get; set; }

        // Notes and History
        public List<ClientNoteDto> Notes { get; set; }

        // Settings
        public UserSettingsDto Settings { get; set; }
    }

    public class PersonalInfoDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public string KycStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string ProfileImageUrl { get; set; }
    }

    public class ClientAccountSummaryDto
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public List<AccountSummaryItemDto> Accounts { get; set; }
        public decimal TotalBalanceEUR { get; set; }
        public Dictionary<string, decimal> BalancesByCurrency { get; set; }
        public string PrimaryCurrency { get; set; }
    }

    public class AccountSummaryItemDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public bool IsPriority { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClientFinancialSummaryDto
    {
        // Assets
        public decimal TotalAssetsEUR { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalInvestments { get; set; }

        // Liabilities
        public decimal TotalLiabilitiesEUR { get; set; }
        public decimal TotalLoanBalance { get; set; }
        public decimal TotalCreditUsed { get; set; }

        // Net Worth
        public decimal NetWorthEUR { get; set; }

        // Income/Expense
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal NetCashFlow { get; set; }

        // Loans
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public decimal MonthlyLoanPayments { get; set; }

        // Investments
        public int ActiveInvestments { get; set; }
        public decimal InvestmentReturns { get; set; }
    }

    public class ClientActivitySummaryDto
    {
        public DateTime? LastLogin { get; set; }
        public DateTime? LastTransaction { get; set; }
        public int TransactionsLast30Days { get; set; }
        public int TotalTransactions { get; set; }
        public decimal VolumeLastMonth { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public string MostUsedService { get; set; }
        public List<RecentActivityItemDto> RecentActivities { get; set; }
    }

    public class RecentActivityItemDto
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ClientRiskProfileDto
    {
        public string RiskLevel { get; set; } // Low, Medium, High
        public int RiskScore { get; set; } // 0-100
        public List<string> RiskFactors { get; set; }
        public int FailedTransactions { get; set; }
        public int DisputedTransactions { get; set; }
        public bool HasOverdueLoans { get; set; }
        public decimal CreditUtilization { get; set; }
        public List<string> Flags { get; set; }
    }

    // Analytics DTOs
    public class ClientAnalyticsDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public AnalyticsPeriodDto Period { get; set; }
        public TransactionAnalyticsDto TransactionAnalytics { get; set; }
        public AccountAnalyticsDto AccountAnalytics { get; set; }
        public BehaviorAnalyticsDto BehaviorAnalytics { get; set; }
        public PerformanceMetricsDto PerformanceMetrics { get; set; }
        public List<MonthlyTrendDto> MonthlyTrends { get; set; }
    }

    public class AnalyticsPeriodDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
    }

    public class TransactionAnalyticsDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public Dictionary<string, int> TransactionsByType { get; set; }
        public Dictionary<string, decimal> VolumeByType { get; set; }
        public List<DailyTransactionDto> DailyTransactions { get; set; }
        public string MostActiveDay { get; set; }
        public string MostActiveHour { get; set; }
    }

    public class AccountAnalyticsDto
    {
        public decimal AverageBalance { get; set; }
        public decimal HighestBalance { get; set; }
        public decimal LowestBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal BalanceGrowthRate { get; set; }
        public List<BalanceTrendDto> BalanceTrend { get; set; }
    }

    public class BehaviorAnalyticsDto
    {
        public int LoginCount { get; set; }
        public decimal AverageSessionDuration { get; set; }
        public List<string> MostUsedFeatures { get; set; }
        public Dictionary<string, int> FeatureUsageCount { get; set; }
        public string PreferredTransactionTime { get; set; }
        public string PreferredDevice { get; set; }
    }

    public class ClientPerformanceMetricsDto
    {
        public decimal CustomerLifetimeValue { get; set; }
        public decimal RetentionRate { get; set; }
        public decimal EngagementScore { get; set; }
        public decimal SatisfactionScore { get; set; }
        public int SupportTickets { get; set; }
        public decimal AverageResponseTime { get; set; }
    }

    public class MonthlyTrendDto
    {
        public string Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int Transactions { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal NetCashFlow { get; set; }
    }

    public class DailyTransactionDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Volume { get; set; }
    }

    public class BalanceTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }

    // Admin Dashboard DTOs
    public class AdminClientDashboardDto
    {
        public DashboardSummaryDto Summary { get; set; }
        public List<ClientSummaryDto> RecentClients { get; set; }
        public List<TopClientDto> TopClientsByBalance { get; set; }
        public List<TopClientDto> TopClientsByTransactions { get; set; }
        public Dictionary<string, int> ClientDistribution { get; set; }
        public List<AlertDto> SystemAlerts { get; set; }
        public SystemHealthDto SystemHealth { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int NewClientsThisMonth { get; set; }
        public decimal TotalAssetsUnderManagement { get; set; }
        public decimal TotalLoanPortfolio { get; set; }
        public decimal TotalInvestmentPortfolio { get; set; }
        public int TotalAccounts { get; set; }
        public int TransactionsToday { get; set; }
        public decimal VolumeToday { get; set; }
    }

    public class TopClientDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public decimal Value { get; set; } // Could be balance, transaction volume, etc.
        public string Metric { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class AlertDto
    {
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid? RelatedClientId { get; set; }
    }

    public class SystemHealthDto
    {
        public string Status { get; set; }
        public int ActiveSessions { get; set; }
        public decimal SystemLoad { get; set; }
        public decimal DatabaseLoad { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }
    }

    // System Statistics DTOs
    public class SystemStatisticsDto
    {
        public ClientStatisticsDto ClientStats { get; set; }
        public TransactionStatisticsDto TransactionStats { get; set; }
        public AccountStatisticsDto AccountStats { get; set; }
        public LoanStatisticsDto LoanStats { get; set; }
        public InvestmentStatisticsDto InvestmentStats { get; set; }
        public RevenueStatisticsDto RevenueStats { get; set; }
    }

    public class ClientStatisticsDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int NewClients { get; set; }
        public int ChurnedClients { get; set; }
        public decimal RetentionRate { get; set; }
        public decimal AcquisitionRate { get; set; }
    }

    public class TransactionStatisticsDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public Dictionary<string, int> TransactionsByType { get; set; }
        public Dictionary<string, decimal> VolumeByType { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class AccountStatisticsDto
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public Dictionary<string, int> AccountsByType { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AverageAccountBalance { get; set; }
    }

    public class LoanStatisticsDto
    {
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public decimal TotalLoanAmount { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public decimal AverageInterestRate { get; set; }
        public int DefaultedLoans { get; set; }
        public decimal DefaultRate { get; set; }
    }

    public class InvestmentStatisticsDto
    {
        public int TotalInvestments { get; set; }
        public int ActiveInvestments { get; set; }
        public decimal TotalInvestedAmount { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal AverageROI { get; set; }
    }

    public class RevenueStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TransactionFees { get; set; }
        public decimal LoanInterest { get; set; }
        public decimal InvestmentFees { get; set; }
        public decimal OtherRevenue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    // Activity Report DTOs
    public class ActivityReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailyActivitySummaryDto> DailyActivities { get; set; }
        public Dictionary<string, int> ActivityByType { get; set; }
        public List<string> MostActiveClients { get; set; }
        public decimal TotalVolume { get; set; }
        public int TotalTransactions { get; set; }
    }

    public class DailyActivitySummaryDto
    {
        public DateTime Date { get; set; }
        public int LoginCount { get; set; }
        public int TransactionCount { get; set; }
        public decimal TransactionVolume { get; set; }
        public int NewAccounts { get; set; }
        public int NewLoans { get; set; }
    }

    // Action DTOs
    public class UpdateClientStatusDto
    {
        [Required]
        public bool IsActive { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
    }

    public class UpdateClientLimitsDto
    {
        public decimal? DailyTransferLimit { get; set; }
        public decimal? DailyWithdrawalLimit { get; set; }
        public decimal? MonthlyTransactionLimit { get; set; }
        public decimal? MaxLoanAmount { get; set; }
        public decimal? MaxInvestmentAmount { get; set; }
    }

    public class AddClientNoteDto
    {
        [Required]
        [MaxLength(1000)]
        public string Note { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } // General, Risk, Compliance, Support, etc.
    }

    public class ClientNoteDto
    {
        public Guid Id { get; set; }
        public string Note { get; set; }
        public string Category { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BulkClientActionDto
    {
        [Required]
        public List<Guid> ClientIds { get; set; }

        [Required]
        public string Action { get; set; } // Activate, Deactivate, UpdateLimits, SendNotification

        public Dictionary<string, object> Parameters { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
    }

    public class BulkActionResultDto
    {
        public int TotalProcessed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<BulkActionItemResultDto> Results { get; set; }
    }

    public class BulkActionItemResultDto
    {
        public Guid ClientId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    // Additional Detail DTOs
    public class AccountDetailsDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public bool IsPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    public class TransactionDetailsDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }