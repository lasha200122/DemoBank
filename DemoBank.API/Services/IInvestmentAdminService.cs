using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IInvestmentAdminService
{
    Task<InvestmentDto> ApproveInvestmentAsync(Guid investmentId, InvestmentApprovalDto dto, string approvedBy);
    Task<InvestmentDto> RejectInvestmentAsync(Guid investmentId, string reason, string rejectedBy);
    Task<List<PendingInvestmentDto>> GetPendingInvestmentsAsync();
    Task<AdminInvestmentDashboardDto> GetAdminDashboardAsync();

    // Rate Management
    Task<bool> UpdateInvestmentRateAsync(UpdateInvestmentRateDto dto, string updatedBy);
    Task<bool> SetUserRateAsync(UserInvestmentRateDto dto, string createdBy);
    Task<bool> BulkUpdateRatesAsync(BulkRateUpdateDto dto, string updatedBy);
    Task<object> GetUserRatesAsync(Guid userId);
    Task<Dictionary<Guid, decimal>> GetAllUserROIsAsync();

    // Plan Management
    Task<InvestmentPlanDto> CreatePlanAsync(CreateInvestmentPlanDto dto, string createdBy);
    Task<InvestmentPlanDto> UpdatePlanAsync(Guid planId, UpdateInvestmentPlanDto dto, string updatedBy);
    Task<bool> DeletePlanAsync(Guid planId);

    // Fund Management
    Task<UserInvestmentOverviewDto> GetUserInvestmentOverviewAsync(Guid userId);
    Task<List<UserInvestmentOverviewDto>> GetAllInvestorsAsync();
    Task<bool> ProcessScheduledPayoutsAsync();
    Task<TotalPayoutsDto> GetPayoutSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> ManualPayoutAsync(Guid investmentId, decimal amount, string processedBy);

    // Analytics & Reports
    Task<AdminInvestmentSummaryDto> GetFundSummaryAsync();
    Task<Dictionary<string, object>> GenerateInvestmentReportAsync(DateTime startDate, DateTime endDate);
    Task<List<InvestmentAlertDto>> GetInvestmentAlertsAsync();
}