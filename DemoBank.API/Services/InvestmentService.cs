using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DemoBank.API.Services;

public class InvestmentService : IInvestmentService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly IInvestmentCalculatorService _calculatorService;
    private readonly ILogger<InvestmentService> _logger;

    public InvestmentService(
        DemoBankContext context,
        IAccountService accountService,
        IInvestmentCalculatorService calculatorService,
        ILogger<InvestmentService> logger)
    {
        _context = context;
        _accountService = accountService;
        _calculatorService = calculatorService;
        _logger = logger;
    }

    public async Task<InvestmentDto> ApplyForInvestmentAsync(Guid userId, CreateInvestmentDto dto)
    {
        // Validate plan exists and is active
        var plan = await _context.InvestmentPlans
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive);

        if (plan == null)
            throw new InvalidOperationException("Investment plan not found or inactive");

        // Validate amount
        if (dto.Amount < plan.MinimumInvestment || dto.Amount > plan.MaximumInvestment)
            throw new InvalidOperationException(
                $"Amount must be between {plan.Currency} {plan.MinimumInvestment:N2} and {plan.Currency} {plan.MaximumInvestment:N2}");

        // Validate term
        if (dto.TermMonths < plan.MinTermMonths || dto.TermMonths > plan.MaxTermMonths)
            throw new InvalidOperationException(
                $"Term must be between {plan.MinTermMonths} and {plan.MaxTermMonths} months");

        // Check if user has sufficient funds
        if (dto.SourceAccountId.HasValue)
        {
            var account = await _accountService.GetByIdAsync(dto.SourceAccountId.Value);
            if (account.UserId != userId)
                throw new UnauthorizedAccessException("You don't own this account");

            if (account.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds in the source account");
        }

        // Calculate ROI based on tier
        var tierRates = JsonSerializer.Deserialize<List<TierRateDto>>(plan.TierRatesJson ?? "[]");
        var applicableRate = plan.BaseROI;

        if (tierRates != null && tierRates.Any())
        {
            var tier = tierRates.FirstOrDefault(t => dto.Amount >= t.MinAmount && dto.Amount <= t.MaxAmount);
            if (tier != null)
                applicableRate = tier.ROI;
        }

        // Check for user-specific rates
        var userRate = await _context.InvestmentRates
            .Where(r => r.UserId == userId &&
                       (r.PlanId == null || r.PlanId == plan.Id) &&
                       r.IsActive &&
                       r.EffectiveFrom <= DateTime.UtcNow &&
                       (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.Rate)
            .FirstOrDefaultAsync();

        if (userRate != null)
        {
            applicableRate = userRate.RateType == "BONUS"
                ? applicableRate + userRate.Rate
                : userRate.Rate;
        }

        // Calculate projections
        var calculator = await _calculatorService.CalculateReturnsAsync(
            dto.Amount,
            dto.TermMonths,
            applicableRate,
            dto.PayoutFrequency,
            dto.Currency
        );

        // Create investment
        var investment = new Investment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = dto.PlanId,
            Amount = dto.Amount,
            Currency = dto.Currency,
            CustomROI = applicableRate,
            BaseROI = plan.BaseROI,
            Status = plan.RequiresApproval ? InvestmentStatus.Pending : InvestmentStatus.Active,
            Term = dto.TermMonths <= 6 ? InvestmentTerm.ShortTerm :
                  dto.TermMonths <= 12 ? InvestmentTerm.MediumTerm : InvestmentTerm.LongTerm,
            TermMonths = dto.TermMonths,
            ProjectedReturn = calculator.TotalPayout,
            TotalPaidOut = 0,
            StartDate = plan.RequiresApproval ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow,
            MaturityDate = DateTime.UtcNow.AddMonths(dto.TermMonths),
            PayoutFrequency = dto.PayoutFrequency,
            AutoRenew = dto.AutoRenew,
            MinimumBalance = dto.Amount,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Investments.Add(investment);

        // If auto-approved and source account specified, deduct funds
        if (!plan.RequiresApproval && dto.SourceAccountId.HasValue)
        {
            var account = await _accountService.GetByIdAsync(dto.SourceAccountId.Value);
            account.Balance -= dto.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            // Create investment transaction
            var transaction = new InvestmentTransaction
            {
                Id = Guid.NewGuid(),
                InvestmentId = investment.Id,
                AccountId = account.Id,
                Type = InvestmentTransactionType.InitialDeposit,
                Amount = dto.Amount,
                Currency = account.Currency,
                BalanceBefore = account.Balance + dto.Amount,
                BalanceAfter = account.Balance,
                Description = $"Investment in {plan.Name}",
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestmentTransactions.Add(transaction);
            investment.Status = InvestmentStatus.Active;
        }

        await _context.SaveChangesAsync();

        return MapToDto(investment, plan);
    }

    private InvestmentDto MapToDto(Investment investment, InvestmentPlan plan = null)
    {
        plan ??= investment.Plan;

        return new InvestmentDto
        {
            Id = investment.Id,
            PlanName = plan?.Name ?? "Unknown Plan",
            Amount = investment.Amount,
            Currency = investment.Currency,
            CurrentROI = investment.CustomROI,
            Status = investment.Status.ToString(),
            TermMonths = investment.TermMonths,
            ProjectedReturn = investment.ProjectedReturn,
            TotalPaidOut = investment.TotalPaidOut,
            CurrentValue = investment.Amount + (investment.TotalPaidOut * 0.8m), // Simplified
            StartDate = investment.StartDate,
            MaturityDate = investment.MaturityDate,
            LastPayoutDate = investment.LastPayoutDate,
            PayoutFrequency = investment.PayoutFrequency.ToString(),
            AutoRenew = investment.AutoRenew,
            EarningsToDate = investment.TotalPaidOut,
            NextPayoutAmount = CalculateNextPayout(investment),
            NextPayoutDate = CalculateNextPayoutDate(investment)
        };
    }

    private decimal CalculateNextPayout(Investment investment)
    {
        // Simplified calculation - implement your logic
        var monthlyRate = investment.CustomROI / 12 / 100;
        return investment.Amount * monthlyRate;
    }

    private DateTime? CalculateNextPayoutDate(Investment investment)
    {
        if (investment.Status != InvestmentStatus.Active)
            return null;

        return investment.PayoutFrequency switch
        {
            PayoutFrequency.Monthly => investment.LastPayoutDate?.AddMonths(1) ?? investment.StartDate.AddMonths(1),
            PayoutFrequency.Quarterly => investment.LastPayoutDate?.AddMonths(3) ?? investment.StartDate.AddMonths(3),
            PayoutFrequency.SemiAnnually => investment.LastPayoutDate?.AddMonths(6) ?? investment.StartDate.AddMonths(6),
            PayoutFrequency.Annually => investment.LastPayoutDate?.AddYears(1) ?? investment.StartDate.AddYears(1),
            _ => investment.MaturityDate
        };
    }
}
