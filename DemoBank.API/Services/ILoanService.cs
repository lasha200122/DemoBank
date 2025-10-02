using DemoBank.Core.DTOs;

namespace DemoBank.API.Services;

public interface ILoanService
{
    Task<LoanApplicationResultDto> ApplyForLoanAsync(Guid userId, LoanApplicationDto applicationDto);
    Task<LoanDto> GetLoanByIdAsync(Guid loanId);
    Task<List<LoanDto>> GetUserLoansAsync(Guid userId);
    Task<List<LoanDto>> GetPendingLoansAsync(); // Admin only
    Task<LoanApprovalResultDto> ApproveLoanAsync(Guid loanId, LoanApprovalDto approvalDto);
    Task<LoanApprovalResultDto> RejectLoanAsync(Guid loanId, string reason);
    Task<PaymentScheduleDto> GetPaymentScheduleAsync(Guid loanId);
    Task<LoanPaymentResultDto> MakeLoanPaymentAsync(Guid userId, Guid loanId, decimal amount);
    Task<LoanPaymentResultDto> ProcessScheduledPaymentAsync(Guid loanId);
    Task<List<LoanPaymentHistoryDto>> GetPaymentHistoryAsync(Guid loanId);
    Task<LoanSummaryDto> GetLoanSummaryAsync(Guid userId);
    Task<decimal> CalculateMonthlyPaymentAsync(decimal amount, decimal interestRate, int termMonths);
    Task<LoanEligibilityDto> CheckLoanEligibilityAsync(Guid userId, decimal requestedAmount);
    Task<bool> SetAutoPaymentAsync(Guid loanId, Guid accountId, bool enable);
}