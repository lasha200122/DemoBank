using System.ComponentModel.DataAnnotations;
using DemoBank.Core.Models;

namespace DemoBank.Core.DTOs;

public class CreateInvestmentDto
{
    [Required]
    public Guid? PlanId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public int TermMonths { get; set; }

    public PayoutFrequency PayoutFrequency { get; set; } = PayoutFrequency.Monthly;

    public bool AutoRenew { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }

    public Guid? SourceAccountId { get; set; }
}

public class InvestmentDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal CurrentROI { get; set; }
    public string Status { get; set; }
    public int TermMonths { get; set; }
    public decimal ProjectedReturn { get; set; }
    public decimal TotalPaidOut { get; set; }
    public decimal CurrentValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public DateTime? LastPayoutDate { get; set; }
    public string PayoutFrequency { get; set; }
    public bool AutoRenew { get; set; }
    public decimal EarningsToDate { get; set; }
    public decimal NextPayoutAmount { get; set; }
    public DateTime? NextPayoutDate { get; set; }
}

public class InvestmentDetailsDto : InvestmentDto
{
    public List<InvestmentReturnDto> RecentReturns { get; set; }
    public InvestmentPerformanceDto Performance { get; set; }
    public Dictionary<string, decimal> Projections { get; set; }
    public string RiskLevel { get; set; }
}

public class InvestmentReturnDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public string Type { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
}

public class InvestmentPerformanceDto
{
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal UnrealizedGains { get; set; }
    public decimal RealizedGains { get; set; }
    public decimal PercentageReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public List<MonthlyPerformanceDto> MonthlyPerformance { get; set; }
}

public class MonthlyPerformanceDto
{
    public string Month { get; set; }
    public decimal Return { get; set; }
    public decimal CumulativeReturn { get; set; }
}

// Calculator DTOs
public class InvestmentCalculatorDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public int TermMonths { get; set; }

    public Guid? PlanId { get; set; }

    public decimal? CustomROI { get; set; }

    public PayoutFrequency PayoutFrequency { get; set; } = PayoutFrequency.Monthly;

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";
}

public class InvestmentCalculatorResultDto
{
    public decimal InitialAmount { get; set; }
    public string Currency { get; set; }
    public int TermMonths { get; set; }
    public decimal AnnualROI { get; set; }
    public decimal EffectiveROI { get; set; } // After compounding
    public string PayoutFrequency { get; set; }

    // Returns
    public decimal MonthlyPayout { get; set; }
    public decimal QuarterlyPayout { get; set; }
    public decimal AnnualPayout { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal FinalValue { get; set; }

    // Breakdown
    public List<PayoutScheduleDto> PayoutSchedule { get; set; }
    public Dictionary<string, decimal> YearlyBreakdown { get; set; }

    // Comparison
    public ComparisonDto Comparison { get; set; }
}

public class PayoutScheduleDto
{
    public int Period { get; set; }
    public DateTime Date { get; set; }
    public decimal Principal { get; set; }
    public decimal Interest { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal Balance { get; set; }
}

public class ComparisonDto
{
    public decimal VsSavingsAccount { get; set; }
    public decimal VsInflation { get; set; }
    public decimal VsStockMarket { get; set; }
}

// Admin Management DTOs
public class InvestmentApprovalDto
{
    [Required]
    public bool Approve { get; set; }

    public decimal? OverrideROI { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }

    public Guid? DisbursementAccountId { get; set; }
}

public class UpdateInvestmentRateDto
{
    [Required]
    public Guid InvestmentId { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal NewROI { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; }

    public bool ApplyToFuturePayouts { get; set; } = true;
}

public class UserInvestmentRateDto
{
    public Guid? UserId { get; set; }
    public Guid? PlanId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RateType { get; set; }

    [Required]
    public decimal Rate { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }
}

public class BulkRateUpdateDto
{
    public List<Guid> UserIds { get; set; }
    public List<Guid> InvestmentIds { get; set; }

    [Required]
    public decimal NewROI { get; set; }

    [Required]
    public DateTime EffectiveFrom { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; }
}

// Plan Management DTOs
public class CreateInvestmentPlanDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    [Required]
    public string Type { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal MinimumInvestment { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal MaximumInvestment { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal BaseROI { get; set; }

    [Required]
    [Range(1, 360)]
    public int MinTermMonths { get; set; }

    [Required]
    [Range(1, 360)]
    public int MaxTermMonths { get; set; }

    public PayoutFrequency DefaultPayoutFrequency { get; set; }

    public bool RequiresApproval { get; set; }

    [Range(0, 100)]
    public decimal EarlyWithdrawalPenalty { get; set; }

    public string RiskLevel { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public List<TierRateDto> TierRates { get; set; }
}

public class UpdateInvestmentPlanDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal? MinimumInvestment { get; set; }
    public decimal? MaximumInvestment { get; set; }
    public decimal? BaseROI { get; set; }
    public int? MinTermMonths { get; set; }
    public int? MaxTermMonths { get; set; }
    public bool? RequiresApproval { get; set; }
    public decimal? EarlyWithdrawalPenalty { get; set; }
    public string RiskLevel { get; set; }
    public bool? IsActive { get; set; }
}

public class TierRateDto
{
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal ROI { get; set; }
}

public class InvestmentPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public decimal MinimumInvestment { get; set; }
    public decimal MaximumInvestment { get; set; }
    public decimal BaseROI { get; set; }
    public int MinTermMonths { get; set; }
    public int MaxTermMonths { get; set; }
    public string DefaultPayoutFrequency { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal EarlyWithdrawalPenalty { get; set; }
    public string RiskLevel { get; set; }
    public string Currency { get; set; }
    public bool IsActive { get; set; }
    public List<TierRateDto> TierRates { get; set; }
    public int ActiveInvestments { get; set; }
    public decimal TotalInvested { get; set; }
}

// Analytics DTOs
public class InvestmentAnalyticsDto
{
    public InvestmentSummaryDto Summary { get; set; }
    public PortfolioDistributionDto Distribution { get; set; }
    public List<InvestmentDto> ActiveInvestments { get; set; }
    public InvestmentPerformanceMetricsDto Performance { get; set; }
    public ProjectionsDto Projections { get; set; }
    public RiskAnalysisDto RiskAnalysis { get; set; }
}

public class InvestmentSummaryDto
{
    public int TotalInvestments { get; set; }
    public int ActiveInvestments { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturns { get; set; }
    public decimal AverageROI { get; set; }
    public decimal WeightedROI { get; set; }
    public Dictionary<string, decimal> InvestmentsByCurrency { get; set; }
    public Dictionary<string, int> InvestmentsByStatus { get; set; }
}

public class PortfolioDistributionDto
{
    public Dictionary<string, decimal> ByPlanType { get; set; }
    public Dictionary<string, decimal> ByRiskLevel { get; set; }
    public Dictionary<string, decimal> ByTerm { get; set; }
    public Dictionary<string, decimal> ByCurrency { get; set; }
}

public class InvestmentPerformanceMetricsDto
{
    public decimal DailyReturn { get; set; }
    public decimal WeeklyReturn { get; set; }
    public decimal MonthlyReturn { get; set; }
    public decimal QuarterlyReturn { get; set; }
    public decimal YearlyReturn { get; set; }
    public decimal AllTimeReturn { get; set; }
    public List<HistoricalPerformanceDto> History { get; set; }
}

public class HistoricalPerformanceDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public decimal Return { get; set; }
    public decimal CumulativeReturn { get; set; }
}

public class ProjectionsDto
{
    public decimal OneMonthProjection { get; set; }
    public decimal ThreeMonthProjection { get; set; }
    public decimal SixMonthProjection { get; set; }
    public decimal OneYearProjection { get; set; }
    public decimal FiveYearProjection { get; set; }
    public Dictionary<string, decimal> MonthlyProjections { get; set; }
}

public class RiskAnalysisDto
{
    public string OverallRiskLevel { get; set; }
    public decimal RiskScore { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public List<string> RiskFactors { get; set; }
    public List<string> Recommendations { get; set; }
}

// Admin Dashboard DTOs
public class AdminInvestmentDashboardDto
{
    public AdminInvestmentSummaryDto Summary { get; set; }
    public List<PendingInvestmentDto> PendingApprovals { get; set; }
    public List<UserInvestmentOverviewDto> TopInvestors { get; set; }
    public Dictionary<string, decimal> FundAllocation { get; set; }
    public TotalPayoutsDto Payouts { get; set; }
    public List<InvestmentAlertDto> Alerts { get; set; }
}

public class AdminInvestmentSummaryDto
{
    public decimal TotalFundsUnderManagement { get; set; }
    public int TotalActiveInvestments { get; set; }
    public int TotalInvestors { get; set; }
    public decimal AverageInvestmentSize { get; set; }
    public decimal TotalPayoutsDue { get; set; }
    public decimal TotalPayoutsThisMonth { get; set; }
    public Dictionary<string, decimal> FundsByPlan { get; set; }
    public Dictionary<string, int> InvestmentsByStatus { get; set; }
}

public class PendingInvestmentDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string PlanName { get; set; }
    public int TermMonths { get; set; }
    public decimal RequestedROI { get; set; }
    public DateTime ApplicationDate { get; set; }
    public int DaysPending { get; set; }
}

public class UserInvestmentOverviewDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public int TotalInvestments { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AverageROI { get; set; }
    public decimal TotalReturns { get; set; }
    public string RiskProfile { get; set; }
}

public class TotalPayoutsDto
{
    public decimal TodayPayouts { get; set; }
    public decimal ThisWeekPayouts { get; set; }
    public decimal ThisMonthPayouts { get; set; }
    public decimal NextMonthProjected { get; set; }
    public List<UpcomingPayoutDto> UpcomingPayouts { get; set; }
}

public class UpcomingPayoutDto
{
    public Guid InvestmentId { get; set; }
    public string UserName { get; set; }
    public decimal Amount { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Type { get; set; }
}

public class InvestmentAlertDto
{
    public string Type { get; set; }
    public string Severity { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? RelatedInvestmentId { get; set; }
}

// Withdrawal DTOs
public class WithdrawInvestmentDto
{
    [Required]
    public Guid InvestmentId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public Guid? DestinationAccountId { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; }
}

public class InvestmentWithdrawalResultDto
{
    public bool Success { get; set; }
    public decimal WithdrawnAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}