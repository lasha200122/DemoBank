using System.Text.Json;
using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

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

        //// Validate amount
        //if (dto.Amount < plan.MinimumInvestment || dto.Amount > plan.MaximumInvestment)
        //    throw new InvalidOperationException(
        //        $"Amount must be between {plan.Currency} {plan.MinimumInvestment:N2} and {plan.Currency} {plan.MaximumInvestment:N2}");

        //// Validate term
        //if (dto.TermMonths < plan.MinTermMonths || dto.TermMonths > plan.MaxTermMonths)
        //    throw new InvalidOperationException(
        //        $"Term must be between {plan.MinTermMonths} and {plan.MaxTermMonths} months");

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
            CreatedAt = DateTime.UtcNow,
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

    public async Task<InvestmentDetailsDto> GetInvestmentDetailsAsync(Guid investmentId, Guid userId)
    {
        var investment = await _context.Investments
            .Include(i => i.Plan)
            .Include(i => i.Returns)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            return null;

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("You don't have access to this investment");

        var dto = MapToDto(investment);

        var detailsDto = new InvestmentDetailsDto
        {
            Id = dto.Id,
            PlanName = dto.PlanName,
            Amount = dto.Amount,
            Currency = dto.Currency,
            CurrentROI = dto.CurrentROI,
            Status = dto.Status,
            TermMonths = dto.TermMonths,
            ProjectedReturn = dto.ProjectedReturn,
            TotalPaidOut = dto.TotalPaidOut,
            CurrentValue = dto.CurrentValue,
            StartDate = dto.StartDate,
            MaturityDate = dto.MaturityDate,
            LastPayoutDate = dto.LastPayoutDate,
            PayoutFrequency = dto.PayoutFrequency,
            AutoRenew = dto.AutoRenew,
            EarningsToDate = dto.EarningsToDate,
            NextPayoutAmount = dto.NextPayoutAmount,
            NextPayoutDate = dto.NextPayoutDate,
            RecentReturns = MapReturnsToDto(investment.Returns.OrderByDescending(r => r.PaymentDate).Take(10)),
            Performance = await GetInvestmentPerformance(investment),
            Projections = await GenerateProjections(investment),
            RiskLevel = investment.Plan?.RiskLevel.ToString() ?? "Medium"
        };

        return detailsDto;
    }

    public async Task<List<InvestmentDto>> GetUserInvestmentsAsync(Guid userId)
    {
        var investments = await _context.Investments
            .Include(i => i.Plan)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return investments.Select(i => MapToDto(i)).ToList();
    }

    public async Task<List<InvestmentDto>> GetActiveInvestmentsAsync(Guid userId)
    {
        var investments = await _context.Investments
            .Include(i => i.Plan)
            .Where(i => i.UserId == userId && i.Status == InvestmentStatus.Active)
            .OrderByDescending(i => i.StartDate)
            .ToListAsync();

        return investments.Select(i => MapToDto(i)).ToList();
    }

    public async Task<InvestmentAnalyticsDto> GetUserAnalyticsAsync(Guid userId)
    {
        var investments = await _context.Investments
            .Include(i => i.Plan)
            .Include(i => i.Returns)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var activeInvestments = investments.Where(i => i.Status == InvestmentStatus.Active).ToList();

        // Summary
        var summary = new InvestmentSummaryDto
        {
            TotalInvestments = investments.Count,
            ActiveInvestments = activeInvestments.Count,
            TotalInvested = activeInvestments.Sum(i => i.Amount),
            CurrentValue = activeInvestments.Sum(i => i.Amount + (i.TotalPaidOut * 0.8m)),
            TotalReturns = investments.Sum(i => i.TotalPaidOut),
            AverageROI = activeInvestments.Any() ? activeInvestments.Average(i => i.CustomROI) : 0,
            WeightedROI = activeInvestments.Any() ?
                activeInvestments.Sum(i => i.CustomROI * i.Amount) / activeInvestments.Sum(i => i.Amount) : 0,
            InvestmentsByCurrency = activeInvestments.GroupBy(i => i.Currency)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount)),
            InvestmentsByStatus = investments.GroupBy(i => i.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Distribution
        var distribution = new PortfolioDistributionDto
        {
            ByPlanType = activeInvestments.Where(i => i.Plan != null)
                .GroupBy(i => i.Plan.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount)),
            ByRiskLevel = activeInvestments.Where(i => i.Plan != null)
                .GroupBy(i => i.Plan.RiskLevel.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount)),
            ByTerm = activeInvestments.GroupBy(i => i.Term.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount)),
            ByCurrency = activeInvestments.GroupBy(i => i.Currency)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount))
        };

        // Performance metrics
        var performance = await GetPortfolioPerformance(activeInvestments);

        // Projections
        var projections = await GetProjectionsAsync(userId);

        // Risk Analysis
        var riskAnalysis = GenerateRiskAnalysis(activeInvestments);

        return new InvestmentAnalyticsDto
        {
            Summary = summary,
            Distribution = distribution,
            ActiveInvestments = activeInvestments.Select(i => MapToDto(i)).ToList(),
            Performance = performance,
            Projections = projections,
            RiskAnalysis = riskAnalysis
        };
    }

    public async Task<InvestmentWithdrawalResultDto> WithdrawInvestmentAsync(Guid userId, WithdrawInvestmentDto dto)
    {
        var investment = await _context.Investments
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == dto.InvestmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("You don't have access to this investment");

        if (investment.Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Can only withdraw from active investments");

        // Check if amount is valid
        if (dto.Amount > investment.Amount - investment.MinimumBalance)
            throw new InvalidOperationException($"Cannot withdraw more than available balance. Maximum: {investment.Amount - investment.MinimumBalance:N2}");

        // Calculate penalty for early withdrawal
        var penalty = await _calculatorService.CalculateEarlyWithdrawalPenaltyAsync(dto.InvestmentId, dto.Amount);
        var netAmount = dto.Amount - penalty;

        // Get destination account
        var destinationAccount = dto.DestinationAccountId.HasValue ?
            await _accountService.GetByIdAsync(dto.DestinationAccountId.Value) :
            await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId && a.IsPriority && a.Currency == investment.Currency);

        if (destinationAccount == null)
            throw new InvalidOperationException("No valid destination account found");

        // Process withdrawal
        investment.Amount -= dto.Amount;

        if (investment.Amount <= investment.MinimumBalance)
        {
            investment.Status = InvestmentStatus.Withdrawn;
        }

        // Credit account
        destinationAccount.Balance += netAmount;
        destinationAccount.UpdatedAt = DateTime.UtcNow;

        // Create transaction
        var transaction = new InvestmentTransaction
        {
            Id = Guid.NewGuid(),
            InvestmentId = investment.Id,
            AccountId = destinationAccount.Id,
            Type = dto.Amount == investment.Amount ?
                InvestmentTransactionType.FullWithdrawal :
                InvestmentTransactionType.PartialWithdrawal,
            Amount = netAmount,
            Currency = investment.Currency,
            BalanceBefore = destinationAccount.Balance - netAmount,
            BalanceAfter = destinationAccount.Balance,
            Description = $"Investment withdrawal - {dto.Reason}",
            CreatedAt = DateTime.UtcNow
        };

        _context.InvestmentTransactions.Add(transaction);

        // Record penalty if applicable
        if (penalty > 0)
        {
            var penaltyReturn = new InvestmentReturn
            {
                Id = Guid.NewGuid(),
                InvestmentId = investment.Id,
                Amount = -penalty,
                Currency = investment.Currency,
                InterestAmount = 0,
                PrincipalAmount = -penalty,
                Type = ReturnType.Penalty,
                PaymentDate = DateTime.UtcNow,
                ProcessedDate = DateTime.UtcNow,
                Status = PaymentStatus.Completed,
                Description = "Early withdrawal penalty",
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestmentReturns.Add(penaltyReturn);
        }

        await _context.SaveChangesAsync();

        return new InvestmentWithdrawalResultDto
        {
            Success = true,
            WithdrawnAmount = dto.Amount,
            PenaltyAmount = penalty,
            NetAmount = netAmount,
            RemainingBalance = investment.Amount,
            Status = investment.Status.ToString(),
            Message = "Withdrawal processed successfully"
        };
    }

    public async Task<List<InvestmentReturnDto>> GetInvestmentReturnsAsync(Guid investmentId, Guid userId)
    {
        var investment = await _context.Investments
            .Include(i => i.Returns)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("You don't have access to this investment");

        return MapReturnsToDto(investment.Returns.OrderByDescending(r => r.PaymentDate));
    }

    // Calculator
    public async Task<InvestmentCalculatorResultDto> CalculateReturnsAsync(InvestmentCalculatorDto dto)
    {
        decimal roi = dto.CustomROI ?? 0;

        if (dto.PlanId.HasValue && !dto.CustomROI.HasValue)
        {
            var plan = await _context.InvestmentPlans.FindAsync(dto.PlanId.Value);
            if (plan != null)
            {
                roi = plan.BaseROI;

                // Check for tier rates
                if (!string.IsNullOrEmpty(plan.TierRatesJson))
                {
                    var tierRates = JsonSerializer.Deserialize<List<TierRateDto>>(plan.TierRatesJson);
                    var tier = tierRates?.FirstOrDefault(t => dto.Amount >= t.MinAmount && dto.Amount <= t.MaxAmount);
                    if (tier != null)
                        roi = tier.ROI;
                }
            }
        }

        return await _calculatorService.CalculateReturnsAsync(
            dto.Amount,
            dto.TermMonths,
            roi,
            dto.PayoutFrequency,
            dto.Currency
        );
    }

    public async Task<ProjectionsDto> GetProjectionsAsync(Guid userId)
    {
        var activeInvestments = await _context.Investments
            .Where(i => i.UserId == userId && i.Status == InvestmentStatus.Active)
            .ToListAsync();

        if (!activeInvestments.Any())
        {
            return new ProjectionsDto
            {
                OneMonthProjection = 0,
                ThreeMonthProjection = 0,
                SixMonthProjection = 0,
                OneYearProjection = 0,
                FiveYearProjection = 0,
                MonthlyProjections = new Dictionary<string, decimal>()
            };
        }

        var currentValue = activeInvestments.Sum(i => i.Amount);
        var weightedROI = activeInvestments.Sum(i => i.CustomROI * i.Amount) / activeInvestments.Sum(i => i.Amount);
        var monthlyRate = weightedROI / 12 / 100;

        var projections = new ProjectionsDto
        {
            OneMonthProjection = currentValue * (1 + monthlyRate),
            ThreeMonthProjection = currentValue * (decimal)Math.Pow((double)(1 + monthlyRate), 3),
            SixMonthProjection = currentValue * (decimal)Math.Pow((double)(1 + monthlyRate), 6),
            OneYearProjection = currentValue * (1 + weightedROI / 100),
            FiveYearProjection = currentValue * (decimal)Math.Pow((double)(1 + weightedROI / 100), 5),
            MonthlyProjections = new Dictionary<string, decimal>()
        };

        // Generate monthly projections for the next 12 months
        for (int month = 1; month <= 12; month++)
        {
            var monthName = DateTime.UtcNow.AddMonths(month).ToString("MMM yyyy");
            projections.MonthlyProjections[monthName] = currentValue * (decimal)Math.Pow((double)(1 + monthlyRate), month);
        }

        return projections;
    }

    // Plans
    public async Task<List<InvestmentPlanDto>> GetAvailablePlansAsync()
    {
        var plans = await _context.InvestmentPlans
            .Include(p => p.Investments)
            .Where(p => p.IsActive)
            .ToListAsync();

        return plans.Select(MapPlanToDto).ToList();
    }

    public async Task<InvestmentPlanDto> GetPlanDetailsAsync(Guid planId)
    {
        var plan = await _context.InvestmentPlans
            .Include(p => p.Investments)
            .FirstOrDefaultAsync(p => p.Id == planId);

        return plan != null ? MapPlanToDto(plan) : null;
    }

    // Helper Methods
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
            CurrentValue = investment.Amount + (investment.TotalPaidOut * 0.8m),
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

    private InvestmentPlanDto MapPlanToDto(InvestmentPlan plan)
    {
        var tierRates = string.IsNullOrEmpty(plan.TierRatesJson) ?
            new List<TierRateDto>() :
            JsonSerializer.Deserialize<List<TierRateDto>>(plan.TierRatesJson);

        return new InvestmentPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Type = plan.Type.ToString(),
            MinimumInvestment = plan.MinimumInvestment,
            MaximumInvestment = plan.MaximumInvestment,
            BaseROI = plan.BaseROI,
            MinTermMonths = plan.MinTermMonths,
            MaxTermMonths = plan.MaxTermMonths,
            DefaultPayoutFrequency = plan.DefaultPayoutFrequency.ToString(),
            RequiresApproval = plan.RequiresApproval,
            EarlyWithdrawalPenalty = plan.EarlyWithdrawalPenalty,
            RiskLevel = plan.RiskLevel.ToString(),
            Currency = plan.Currency,
            IsActive = plan.IsActive,
            TierRates = tierRates,
            ActiveInvestments = plan.Investments?.Count(i => i.Status == InvestmentStatus.Active) ?? 0,
            TotalInvested = plan.Investments?.Where(i => i.Status == InvestmentStatus.Active).Sum(i => i.Amount) ?? 0
        };
    }

    private List<InvestmentReturnDto> MapReturnsToDto(IEnumerable<InvestmentReturn> returns)
    {
        return returns.Select(r => new InvestmentReturnDto
        {
            Id = r.Id,
            Amount = r.Amount,
            Currency = r.Currency,
            InterestAmount = r.InterestAmount,
            PrincipalAmount = r.PrincipalAmount,
            Type = r.Type.ToString(),
            PaymentDate = r.PaymentDate,
            Status = r.Status.ToString(),
            Description = r.Description
        }).ToList();
    }

    private decimal CalculateNextPayout(Investment investment)
    {
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

    private async Task<InvestmentPerformanceDto> GetInvestmentPerformance(Investment investment)
    {
        var returns = await _context.InvestmentReturns
            .Where(r => r.InvestmentId == investment.Id)
            .OrderBy(r => r.PaymentDate)
            .ToListAsync();

        var totalReturns = returns.Sum(r => r.Amount);
        var currentValue = investment.Amount + totalReturns;
        var percentageReturn = investment.Amount > 0 ? ((currentValue - investment.Amount) / investment.Amount) * 100 : 0;

        // Calculate annualized return
        var daysSinceStart = (DateTime.UtcNow - investment.StartDate).TotalDays;
        var yearsElapsed = daysSinceStart / 365;
        var annualizedReturn = yearsElapsed > 0 ?
            (decimal)(Math.Pow((double)(currentValue / investment.Amount), 1 / yearsElapsed) - 1) * 100 : 0;

        // Generate monthly performance
        var monthlyPerformance = new List<MonthlyPerformanceDto>();
        var cumulativeReturn = 0m;

        for (int i = 0; i < Math.Min(12, (DateTime.UtcNow - investment.StartDate).TotalDays / 30); i++)
        {
            var monthStart = investment.StartDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var monthReturns = returns
                .Where(r => r.PaymentDate >= monthStart && r.PaymentDate < monthEnd)
                .Sum(r => r.Amount);

            cumulativeReturn += monthReturns;

            monthlyPerformance.Add(new MonthlyPerformanceDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Return = monthReturns,
                CumulativeReturn = cumulativeReturn
            });
        }

        return new InvestmentPerformanceDto
        {
            TotalInvested = investment.Amount,
            CurrentValue = currentValue,
            TotalReturns = totalReturns,
            UnrealizedGains = investment.ProjectedReturn - investment.Amount - totalReturns,
            RealizedGains = totalReturns,
            PercentageReturn = percentageReturn,
            AnnualizedReturn = annualizedReturn,
            MonthlyPerformance = monthlyPerformance
        };
    }

    private async Task<Dictionary<string, decimal>> GenerateProjections(Investment investment)
    {
        var projections = new Dictionary<string, decimal>();
        var monthlyRate = investment.CustomROI / 12 / 100;
        var remainingMonths = (int)((investment.MaturityDate - DateTime.UtcNow).TotalDays / 30);

        for (int i = 1; i <= Math.Min(remainingMonths, 12); i++)
        {
            var projectedValue = investment.Amount * (decimal)Math.Pow((double)(1 + monthlyRate), i);
            projections[$"{i} Month{(i > 1 ? "s" : "")}"] = projectedValue;
        }

        projections["Maturity"] = investment.ProjectedReturn;

        return projections;
    }

    private async Task<InvestmentPerformanceMetricsDto> GetPortfolioPerformance(List<Investment> investments)
    {
        if (!investments.Any())
        {
            return new InvestmentPerformanceMetricsDto
            {
                DailyReturn = 0,
                WeeklyReturn = 0,
                MonthlyReturn = 0,
                QuarterlyReturn = 0,
                YearlyReturn = 0,
                AllTimeReturn = 0,
                History = new List<HistoricalPerformanceDto>()
            };
        }

        var totalInvested = investments.Sum(i => i.Amount);
        var weightedROI = investments.Sum(i => i.CustomROI * i.Amount) / totalInvested;
        var dailyRate = weightedROI / 365 / 100;
        var weeklyRate = weightedROI / 52 / 100;
        var monthlyRate = weightedROI / 12 / 100;
        var quarterlyRate = weightedROI / 4 / 100;

        // Get all returns
        var returns = await _context.InvestmentReturns
            .Where(r => investments.Select(i => i.Id).Contains(r.InvestmentId))
            .OrderBy(r => r.PaymentDate)
            .ToListAsync();

        // Calculate historical performance
        var history = new List<HistoricalPerformanceDto>();
        var startDate = investments.Min(i => i.StartDate);
        var currentDate = startDate;
        var cumulativeReturn = 0m;

        while (currentDate < DateTime.UtcNow)
        {
            var periodReturns = returns
                .Where(r => r.PaymentDate >= currentDate && r.PaymentDate < currentDate.AddMonths(1))
                .Sum(r => r.Amount);

            cumulativeReturn += periodReturns;

            history.Add(new HistoricalPerformanceDto
            {
                Date = currentDate,
                Value = totalInvested + cumulativeReturn,
                Return = periodReturns,
                CumulativeReturn = cumulativeReturn
            });

            currentDate = currentDate.AddMonths(1);
        }

        return new InvestmentPerformanceMetricsDto
        {
            DailyReturn = totalInvested * dailyRate,
            WeeklyReturn = totalInvested * weeklyRate,
            MonthlyReturn = totalInvested * monthlyRate,
            QuarterlyReturn = totalInvested * quarterlyRate,
            YearlyReturn = totalInvested * (weightedROI / 100),
            AllTimeReturn = returns.Sum(r => r.Amount),
            History = history
        };
    }

    private RiskAnalysisDto GenerateRiskAnalysis(List<Investment> investments)
    {
        if (!investments.Any())
        {
            return new RiskAnalysisDto
            {
                OverallRiskLevel = "Unknown",
                RiskScore = 0,
                Volatility = 0,
                SharpeRatio = 0,
                MaxDrawdown = 0,
                RiskFactors = new List<string>(),
                Recommendations = new List<string>()
            };
        }

        // Calculate risk metrics
        var riskScores = investments
            .Where(i => i.Plan != null)
            .Select(i => (int)i.Plan.RiskLevel)
            .ToList();

        var avgRiskScore = riskScores.Any() ? riskScores.Average() : 2;
        var volatility = investments
            .Where(i => i.Plan != null)
            .Select(i => i.Plan.VolatilityIndex)
            .DefaultIfEmpty(0)
            .Average();

        // Calculate Sharpe Ratio (simplified)
        var avgReturn = investments.Average(i => i.CustomROI);
        var riskFreeRate = 2m; // Assume 2% risk-free rate
        var stdDeviation = CalculateStandardDeviation(investments.Select(i => i.CustomROI).ToList());
        var sharpeRatio = stdDeviation > 0 ? (avgReturn - riskFreeRate) / stdDeviation : 0;

        // Determine risk factors
        var riskFactors = new List<string>();

        if (avgRiskScore > 3)
            riskFactors.Add("High concentration in risky assets");

        if (investments.Count < 3)
            riskFactors.Add("Low diversification");

        if (investments.Any(i => i.TermMonths > 36))
            riskFactors.Add("Long-term commitments may affect liquidity");

        if (investments.GroupBy(i => i.Currency).Count() == 1)
            riskFactors.Add("Single currency exposure");

        // Generate recommendations
        var recommendations = new List<string>();

        if (avgRiskScore > 3)
            recommendations.Add("Consider balancing with lower-risk investments");

        if (investments.Count < 5)
            recommendations.Add("Increase portfolio diversification");

        if (!investments.Any(i => i.TermMonths <= 12))
            recommendations.Add("Add short-term investments for better liquidity");

        if (volatility > 50)
            recommendations.Add("Consider more stable investment options");

        return new RiskAnalysisDto
        {
            OverallRiskLevel = DetermineRiskLevel(avgRiskScore),
            RiskScore = (decimal)avgRiskScore * 20, // Convert to 0-100 scale
            Volatility = volatility,
            SharpeRatio = sharpeRatio,
            MaxDrawdown = CalculateMaxDrawdown(investments),
            RiskFactors = riskFactors,
            Recommendations = recommendations
        };
    }

    private string DetermineRiskLevel(double score)
    {
        return score switch
        {
            < 1 => "Very Low",
            < 2 => "Low",
            < 3 => "Medium",
            < 4 => "High",
            _ => "Very High"
        };
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
            return 0;

        var avg = values.Average();
        var sum = values.Sum(d => Math.Pow((double)(d - avg), 2));
        return (decimal)Math.Sqrt(sum / (values.Count - 1));
    }

    private decimal CalculateMaxDrawdown(List<Investment> investments)
    {
        // Simplified max drawdown calculation
        // In a real scenario, this would analyze historical performance data
        var totalValue = investments.Sum(i => i.Amount);
        var potentialLoss = investments.Sum(i => i.Amount * (i.Plan?.EarlyWithdrawalPenalty ?? 5) / 100);

        return totalValue > 0 ? (potentialLoss / totalValue) * 100 : 0;
    }
}