using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IInvestmentCalculatorService
{
    Task<InvestmentCalculatorResultDto> CalculateReturnsAsync(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency,
        string currency = "EUR"
    );

    Task<Dictionary<string, InvestmentCalculatorResultDto>> CompareInvestmentPlansAsync(
        decimal amount,
        int termMonths,
        List<Guid> planIds
    );

    Task<PayoutScheduleDto> GeneratePayoutScheduleAsync(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency,
        DateTime startDate
    );

    Task<decimal> CalculateEarlyWithdrawalPenaltyAsync(
        Guid investmentId,
        decimal withdrawalAmount
    );

    Task<ComparisonDto> GenerateComparisonAsync(
        decimal amount,
        decimal returns,
        int termMonths
    );
}
