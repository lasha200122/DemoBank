using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface IInvestmentService
{
    Task<InvestmentDto> ApplyForInvestmentAsync(Guid userId, CreateInvestmentDto dto);
    Task<InvestmentDetailsDto> GetInvestmentDetailsAsync(Guid investmentId, Guid userId);
    Task<List<InvestmentDto>> GetUserInvestmentsAsync(Guid userId);
    Task<List<InvestmentDto>> GetActiveInvestmentsAsync(Guid userId);
    Task<InvestmentAnalyticsDto> GetUserAnalyticsAsync(Guid userId);
    Task<InvestmentWithdrawalResultDto> WithdrawInvestmentAsync(Guid userId, WithdrawInvestmentDto dto);
    Task<List<InvestmentReturnDto>> GetInvestmentReturnsAsync(Guid investmentId, Guid userId);

    // Calculator
    Task<InvestmentCalculatorResultDto> CalculateReturnsAsync(InvestmentCalculatorDto dto);
    Task<ProjectionsDto> GetProjectionsAsync(Guid userId);

    // Plans
    Task<List<InvestmentPlanDto>> GetAvailablePlansAsync();
    Task<InvestmentPlanDto> GetPlanDetailsAsync(Guid planId);
}