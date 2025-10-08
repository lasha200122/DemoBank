using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DemoBank.API.Services;

public class InvestmentAdminService : IInvestmentAdminService
{
    private readonly DemoBankContext _context;
    private readonly ILogger<InvestmentAdminService> _logger;
    private readonly IAccountService _accountService;

    public InvestmentAdminService(
        DemoBankContext context,
        ILogger<InvestmentAdminService> logger,
        IAccountService accountService)
    {
        _context = context;
        _logger = logger;
        _accountService = accountService;
    }

    // Investment Management
    public async Task<InvestmentDto> ApproveInvestmentAsync(Guid investmentId, InvestmentApprovalDto dto, string approvedBy)
    {
        var investment = await _context.Investments
            .Include(i => i.Plan)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.Status != InvestmentStatus.Pending)
            throw new InvalidOperationException($"Investment is not in pending status. Current status: {investment.Status}");

        // Update investment
        investment.Status = InvestmentStatus.Active;
        investment.ApprovedDate = DateTime.UtcNow;
        investment.ApprovedBy = approvedBy;

        if (dto.OverrideROI.HasValue)
        {
            investment.CustomROI = dto.OverrideROI.Value;
            // Recalculate projected returns
            investment.ProjectedReturn = CalculateProjectedReturn(investment.Amount, investment.CustomROI, investment.TermMonths);
        }

        if (!string.IsNullOrEmpty(dto.Notes))
        {
            investment.Notes = $"{investment.Notes}\n[Approval Note]: {dto.Notes}";
        }

        // Process fund disbursement if account specified
        if (dto.DisbursementAccountId.HasValue)
        {
            var account = await _accountService.GetByIdAsync(dto.DisbursementAccountId.Value);
            if (account.UserId != investment.UserId)
                throw new InvalidOperationException("Disbursement account doesn't belong to the investor");

            // Deduct from account
            account.Balance -= investment.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            // Create transaction record
            var transaction = new InvestmentTransaction
            {
                Id = Guid.NewGuid(),
                InvestmentId = investment.Id,
                AccountId = account.Id,
                Type = InvestmentTransactionType.InitialDeposit,
                Amount = investment.Amount,
                Currency = investment.Currency,
                BalanceBefore = account.Balance + investment.Amount,
                BalanceAfter = account.Balance,
                Description = $"Investment approved - {investment.Plan?.Name}",
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestmentTransactions.Add(transaction);
        }

        await _context.SaveChangesAsync();

        return MapToDto(investment);
    }

    public async Task<InvestmentDto> RejectInvestmentAsync(Guid investmentId, string reason, string rejectedBy)
    {
        var investment = await _context.Investments
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.Status != InvestmentStatus.Pending)
            throw new InvalidOperationException($"Investment is not in pending status. Current status: {investment.Status}");

        investment.Status = InvestmentStatus.Rejected;
        investment.RejectedDate = DateTime.UtcNow;
        investment.RejectionReason = reason;
        investment.Notes = $"{investment.Notes}\n[Rejected by {rejectedBy}]: {reason}";

        await _context.SaveChangesAsync();

        return MapToDto(investment);
    }

    public async Task<List<PendingInvestmentDto>> GetPendingInvestmentsAsync()
    {
        var pending = await _context.Investments
            .Include(i => i.User)
            .Include(i => i.Plan)
            .Where(i => i.Status == InvestmentStatus.Pending)
            .OrderBy(i => i.CreatedAt)
            .Select(i => new PendingInvestmentDto
            {
                Id = i.Id,
                UserName = $"{i.User.FirstName} {i.User.LastName}",
                UserEmail = i.User.Email,
                Amount = i.Amount,
                Currency = i.Currency,
                PlanName = i.Plan.Name,
                TermMonths = i.TermMonths,
                RequestedROI = i.CustomROI,
                ApplicationDate = i.CreatedAt,
                DaysPending = (int)(DateTime.UtcNow - i.CreatedAt).TotalDays
            })
            .ToListAsync();

        return pending;
    }

    public async Task<AdminInvestmentDashboardDto> GetAdminDashboardAsync()
    {
        var summary = await GetFundSummaryAsync();
        var pending = await GetPendingInvestmentsAsync();
        var topInvestors = await GetTopInvestorsAsync(10);
        var payouts = await GetPayoutSummaryAsync();
        var alerts = await GetInvestmentAlertsAsync();

        var fundAllocation = await _context.Investments
            .Where(i => i.Status == InvestmentStatus.Active)
            .Include(i => i.Plan)
            .GroupBy(i => i.Plan.Name)
            .Select(g => new { Plan = g.Key, Total = g.Sum(i => i.Amount) })
            .ToDictionaryAsync(x => x.Plan, x => x.Total);

        return new AdminInvestmentDashboardDto
        {
            Summary = summary,
            PendingApprovals = pending,
            TopInvestors = topInvestors,
            FundAllocation = fundAllocation,
            Payouts = payouts,
            Alerts = alerts
        };
    }

    // Rate Management
    public async Task<bool> UpdateInvestmentRateAsync(UpdateInvestmentRateDto dto, string updatedBy)
    {
        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == dto.InvestmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Can only update rates for active investments");

        // Create new rate record
        var rate = new InvestmentRate
        {
            Id = Guid.NewGuid(),
            UserId = investment.UserId,
            PlanId = investment.PlanId,
            RateType = "CUSTOM",
            Rate = dto.NewROI,
            EffectiveFrom = dto.EffectiveFrom,
            IsActive = true,
            Notes = dto.Reason,
            CreatedBy = updatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.InvestmentRates.Add(rate);

        // Update investment ROI if effective immediately
        if (dto.EffectiveFrom <= DateTime.UtcNow && dto.ApplyToFuturePayouts)
        {
            investment.CustomROI = dto.NewROI;
            investment.UpdatedAt = DateTime.UtcNow;

            // Recalculate projected returns
            investment.ProjectedReturn = CalculateProjectedReturn(investment.Amount, dto.NewROI, investment.TermMonths);
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetUserRateAsync(UserInvestmentRateDto dto, string createdBy)
    {
        // Deactivate existing rates if applicable
        if (dto.UserId.HasValue)
        {
            var existingRates = await _context.InvestmentRates
                .Where(r => r.UserId == dto.UserId &&
                           r.PlanId == dto.PlanId &&
                           r.IsActive)
                .ToListAsync();

            foreach (var existingRate in existingRates)
            {
                existingRate.IsActive = false;
                existingRate.EffectiveTo = dto.EffectiveFrom.AddSeconds(-1);
            }
        }

        var rate = new InvestmentRate
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            PlanId = dto.PlanId,
            RateType = dto.RateType,
            Rate = dto.Rate,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsActive = true,
            Notes = dto.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.InvestmentRates.Add(rate);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> BulkUpdateRatesAsync(BulkRateUpdateDto dto, string updatedBy)
    {
        var investments = await _context.Investments
            .Where(i => (dto.UserIds.Contains(i.UserId) || dto.InvestmentIds.Contains(i.Id)) &&
                       i.Status == InvestmentStatus.Active)
            .ToListAsync();

        foreach (var investment in investments)
        {
            // Create rate record
            var rate = new InvestmentRate
            {
                Id = Guid.NewGuid(),
                UserId = investment.UserId,
                PlanId = investment.PlanId,
                RateType = "BULK_UPDATE",
                Rate = dto.NewROI,
                EffectiveFrom = dto.EffectiveFrom,
                IsActive = true,
                Notes = dto.Reason,
                CreatedBy = updatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestmentRates.Add(rate);

            // Update investment if effective immediately
            if (dto.EffectiveFrom <= DateTime.UtcNow)
            {
                investment.CustomROI = dto.NewROI;
                investment.UpdatedAt = DateTime.UtcNow;
                investment.ProjectedReturn = CalculateProjectedReturn(investment.Amount, dto.NewROI, investment.TermMonths);
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<object> GetUserRatesAsync(Guid userId)
    {
        var rates = await _context.InvestmentRates
            .Where(r => r.UserId == userId && r.IsActive)
            .Include(r => r.Plan)
            .Select(r => new
            {
                r.Id,
                PlanName = r.Plan != null ? r.Plan.Name : "All Plans",
                r.RateType,
                r.Rate,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.CreatedBy,
                r.CreatedAt
            })
            .ToListAsync();

        return rates;
    }

    public async Task<Dictionary<Guid, decimal>> GetAllUserROIsAsync()
    {
        var userRois = await _context.Investments
            .Where(i => i.Status == InvestmentStatus.Active)
            .GroupBy(i => i.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                WeightedROI = g.Sum(i => i.CustomROI * i.Amount) / g.Sum(i => i.Amount)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.WeightedROI);

        return userRois;
    }

    // Plan Management
    public async Task<InvestmentPlanDto> CreatePlanAsync(CreateInvestmentPlanDto dto, string createdBy)
    {
        var plan = new InvestmentPlan
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Type = Enum.Parse<InvestmentPlanType>(dto.Type),
            MinimumInvestment = dto.MinimumInvestment,
            MaximumInvestment = dto.MaximumInvestment,
            BaseROI = dto.BaseROI,
            MinTermMonths = dto.MinTermMonths,
            MaxTermMonths = dto.MaxTermMonths,
            DefaultPayoutFrequency = dto.DefaultPayoutFrequency,
            RequiresApproval = dto.RequiresApproval,
            EarlyWithdrawalPenalty = dto.EarlyWithdrawalPenalty,
            RiskLevel = Enum.Parse<RiskLevel>(dto.RiskLevel ?? "Medium"),
            Currency = dto.Currency,
            IsActive = true,
            TierRatesJson = dto.TierRates != null ? JsonSerializer.Serialize(dto.TierRates) : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.InvestmentPlans.Add(plan);
        await _context.SaveChangesAsync();

        return MapPlanToDto(plan);
    }

    public async Task<InvestmentPlanDto> UpdatePlanAsync(Guid planId, UpdateInvestmentPlanDto dto, string updatedBy)
    {
        var plan = await _context.InvestmentPlans
            .Include(p => p.Investments)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null)
            throw new InvalidOperationException("Investment plan not found");

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.Name))
            plan.Name = dto.Name;

        if (!string.IsNullOrEmpty(dto.Description))
            plan.Description = dto.Description;

        if (dto.MinimumInvestment.HasValue)
            plan.MinimumInvestment = dto.MinimumInvestment.Value;

        if (dto.MaximumInvestment.HasValue)
            plan.MaximumInvestment = dto.MaximumInvestment.Value;

        if (dto.BaseROI.HasValue)
            plan.BaseROI = dto.BaseROI.Value;

        if (dto.MinTermMonths.HasValue)
            plan.MinTermMonths = dto.MinTermMonths.Value;

        if (dto.MaxTermMonths.HasValue)
            plan.MaxTermMonths = dto.MaxTermMonths.Value;

        if (dto.RequiresApproval.HasValue)
            plan.RequiresApproval = dto.RequiresApproval.Value;

        if (dto.EarlyWithdrawalPenalty.HasValue)
            plan.EarlyWithdrawalPenalty = dto.EarlyWithdrawalPenalty.Value;

        if (!string.IsNullOrEmpty(dto.RiskLevel))
            plan.RiskLevel = Enum.Parse<RiskLevel>(dto.RiskLevel);

        if (dto.IsActive.HasValue)
            plan.IsActive = dto.IsActive.Value;

        plan.UpdatedAt = DateTime.UtcNow;
        plan.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        return MapPlanToDto(plan);
    }

    public async Task<bool> DeletePlanAsync(Guid planId)
    {
        var plan = await _context.InvestmentPlans
            .Include(p => p.Investments)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null)
            return false;

        // Check if there are active investments
        var hasActiveInvestments = plan.Investments.Any(i =>
            i.Status == InvestmentStatus.Active ||
            i.Status == InvestmentStatus.Pending);

        if (hasActiveInvestments)
            throw new InvalidOperationException("Cannot delete plan with active or pending investments");

        // Soft delete - just mark as inactive
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    // Fund Management
    public async Task<UserInvestmentOverviewDto> GetUserInvestmentOverviewAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var investments = await _context.Investments
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var activeInvestments = investments.Where(i => i.Status == InvestmentStatus.Active).ToList();

        return new UserInvestmentOverviewDto
        {
            UserId = userId,
            UserName = $"{user.FirstName} {user.LastName}",
            UserEmail = user.Email,
            TotalInvestments = investments.Count,
            TotalInvested = activeInvestments.Sum(i => i.Amount),
            CurrentValue = activeInvestments.Sum(i => i.Amount + (i.TotalPaidOut * 0.8m)),
            AverageROI = activeInvestments.Any() ?
                activeInvestments.Average(i => i.CustomROI) : 0,
            TotalReturns = investments.Sum(i => i.TotalPaidOut),
            RiskProfile = DetermineRiskProfile(activeInvestments)
        };
    }

    public async Task<List<UserInvestmentOverviewDto>> GetAllInvestorsAsync()
    {
        var investors = await _context.Investments
            .Include(i => i.User)
            .GroupBy(i => i.User)
            .Select(g => new UserInvestmentOverviewDto
            {
                UserId = g.Key.Id,
                UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                UserEmail = g.Key.Email,
                TotalInvestments = g.Count(),
                TotalInvested = g.Where(i => i.Status == InvestmentStatus.Active).Sum(i => i.Amount),
                CurrentValue = g.Where(i => i.Status == InvestmentStatus.Active)
                    .Sum(i => i.Amount + (i.TotalPaidOut * 0.8m)),
                AverageROI = g.Where(i => i.Status == InvestmentStatus.Active).Any() ?
                    g.Where(i => i.Status == InvestmentStatus.Active).Average(i => i.CustomROI) : 0,
                TotalReturns = g.Sum(i => i.TotalPaidOut)
            })
            .OrderByDescending(i => i.TotalInvested)
            .ToListAsync();

        return investors;
    }

    public async Task<bool> ProcessScheduledPayoutsAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Get all active investments with scheduled payouts
        var investments = await _context.Investments
            .Include(i => i.User)
            .Where(i => i.Status == InvestmentStatus.Active)
            .ToListAsync();

        foreach (var investment in investments)
        {
            var nextPayoutDate = CalculateNextPayoutDate(investment);

            if (nextPayoutDate.HasValue && nextPayoutDate.Value.Date <= today)
            {
                await ProcessPayout(investment);
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TotalPayoutsDto> GetPayoutSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.Date;
        endDate ??= DateTime.UtcNow.AddMonths(1);

        var payouts = await _context.InvestmentReturns
            .Where(r => r.PaymentDate >= startDate && r.PaymentDate <= endDate)
            .ToListAsync();

        var todayPayouts = payouts.Where(p => p.PaymentDate.Date == DateTime.UtcNow.Date);
        var weekPayouts = payouts.Where(p => p.PaymentDate >= DateTime.UtcNow.AddDays(-7));
        var monthPayouts = payouts.Where(p => p.PaymentDate.Month == DateTime.UtcNow.Month);

        var upcoming = await _context.Investments
            .Include(i => i.User)
            .Where(i => i.Status == InvestmentStatus.Active)
            .Select(i => new { Investment = i, NextPayout = CalculateNextPayoutDate(i) })
            .Where(x => x.NextPayout.HasValue && x.NextPayout.Value <= endDate)
            .Select(x => new UpcomingPayoutDto
            {
                InvestmentId = x.Investment.Id,
                UserName = $"{x.Investment.User.FirstName} {x.Investment.User.LastName}",
                Amount = CalculatePayoutAmount(x.Investment),
                ScheduledDate = x.NextPayout.Value,
                Type = x.Investment.PayoutFrequency.ToString()
            })
            .ToListAsync();

        return new TotalPayoutsDto
        {
            TodayPayouts = (decimal)todayPayouts.Sum(p => p.Amount),
            ThisWeekPayouts = (decimal)weekPayouts.Sum(p => p.Amount),
            ThisMonthPayouts = (decimal)monthPayouts.Sum(p => p.Amount),
            NextMonthProjected = upcoming.Sum(u => u.Amount),
            UpcomingPayouts = upcoming.OrderBy(u => u.ScheduledDate).Take(10).ToList()
        };
    }

    public async Task<bool> ManualPayoutAsync(Guid investmentId, decimal amount, string processedBy)
    {
        var investment = await _context.Investments
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        if (investment.Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Can only process payouts for active investments");

        // Create payout record
        var payout = new InvestmentReturn
        {
            Id = Guid.NewGuid(),
            InvestmentId = investmentId,
            Amount = amount,
            Currency = investment.Currency,
            InterestAmount = amount,
            PrincipalAmount = 0,
            Type = ReturnType.Bonus,
            PaymentDate = DateTime.UtcNow,
            ProcessedDate = DateTime.UtcNow,
            Status = PaymentStatus.Completed,
            Description = $"Manual payout processed by {processedBy}",
            CreatedAt = DateTime.UtcNow
        };

        _context.InvestmentReturns.Add(payout);

        // Update investment
        investment.TotalPaidOut += amount;
        investment.LastPayoutDate = DateTime.UtcNow;

        // Credit user account
        var userAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == investment.UserId &&
                                    a.Currency == investment.Currency &&
                                    a.IsPriority);

        if (userAccount != null)
        {
            userAccount.Balance += amount;
            userAccount.UpdatedAt = DateTime.UtcNow;

            // Create transaction
            var transaction = new InvestmentTransaction
            {
                Id = Guid.NewGuid(),
                InvestmentId = investmentId,
                AccountId = userAccount.Id,
                Type = InvestmentTransactionType.InterestPayout,
                Amount = amount,
                Currency = investment.Currency,
                BalanceBefore = userAccount.Balance - amount,
                BalanceAfter = userAccount.Balance,
                Description = $"Manual investment payout",
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestmentTransactions.Add(transaction);
        }

        await _context.SaveChangesAsync();

        return true;
    }

    // Analytics & Reports
    public async Task<AdminInvestmentSummaryDto> GetFundSummaryAsync()
    {
        var activeInvestments = await _context.Investments
            .Where(i => i.Status == InvestmentStatus.Active)
            .Include(i => i.Plan)
            .ToListAsync();

        var allInvestments = await _context.Investments.ToListAsync();

        var fundsByPlan = activeInvestments
            .GroupBy(i => i.Plan?.Name ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        var investmentsByStatus = allInvestments
            .GroupBy(i => i.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var payoutsDue = activeInvestments
            .Where(i => CalculateNextPayoutDate(i) <= nextMonth)
            .Sum(i => CalculatePayoutAmount(i));

        var thisMonthPayouts = await _context.InvestmentReturns
            .Where(r => r.PaymentDate.Month == DateTime.UtcNow.Month &&
                       r.PaymentDate.Year == DateTime.UtcNow.Year)
            .SumAsync(r => r.Amount);

        return new AdminInvestmentSummaryDto
        {
            TotalFundsUnderManagement = activeInvestments.Sum(i => i.Amount),
            TotalActiveInvestments = activeInvestments.Count,
            TotalInvestors = activeInvestments.Select(i => i.UserId).Distinct().Count(),
            AverageInvestmentSize = activeInvestments.Any() ? activeInvestments.Average(i => i.Amount) : 0,
            TotalPayoutsDue = payoutsDue,
            TotalPayoutsThisMonth = thisMonthPayouts,
            FundsByPlan = fundsByPlan,
            InvestmentsByStatus = investmentsByStatus
        };
    }

    public async Task<Dictionary<string, object>> GenerateInvestmentReportAsync(DateTime startDate, DateTime endDate)
    {
        var report = new Dictionary<string, object>();

        // New investments
        var newInvestments = await _context.Investments
            .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate)
            .ToListAsync();

        report["NewInvestments"] = new
        {
            Count = newInvestments.Count,
            TotalAmount = newInvestments.Sum(i => i.Amount),
            AverageAmount = newInvestments.Any() ? newInvestments.Average(i => i.Amount) : 0
        };

        // Payouts
        var payouts = await _context.InvestmentReturns
            .Where(r => r.PaymentDate >= startDate && r.PaymentDate <= endDate)
            .ToListAsync();

        report["Payouts"] = new
        {
            Count = payouts.Count,
            TotalAmount = payouts.Sum(p => p.Amount),
            InterestPaid = payouts.Sum(p => p.InterestAmount),
            PrincipalReturned = payouts.Sum(p => p.PrincipalAmount)
        };

        // Performance metrics
        var activeInvestments = await _context.Investments
            .Where(i => i.Status == InvestmentStatus.Active)
            .ToListAsync();

        report["Performance"] = new
        {
            AverageROI = activeInvestments.Any() ? activeInvestments.Average(i => i.CustomROI) : 0,
            TotalUnderManagement = activeInvestments.Sum(i => i.Amount),
            WeightedAverageROI = activeInvestments.Any() ?
                activeInvestments.Sum(i => i.CustomROI * i.Amount) / activeInvestments.Sum(i => i.Amount) : 0
        };

        // User activity
        var activeUsers = newInvestments.Select(i => i.UserId).Distinct().Count();
        report["UserActivity"] = new
        {
            NewInvestors = activeUsers,
            TotalActiveInvestors = activeInvestments.Select(i => i.UserId).Distinct().Count()
        };

        return report;
    }

    public async Task<List<InvestmentAlertDto>> GetInvestmentAlertsAsync()
    {
        var alerts = new List<InvestmentAlertDto>();

        // Check for overdue payouts
        var overduePayouts = await _context.InvestmentReturns
            .Where(r => r.Status == PaymentStatus.Scheduled &&
                       r.PaymentDate < DateTime.UtcNow)
            .CountAsync();

        if (overduePayouts > 0)
        {
            alerts.Add(new InvestmentAlertDto
            {
                Type = "Payment",
                Severity = "High",
                Message = $"{overduePayouts} overdue payouts require attention",
                Timestamp = DateTime.UtcNow
            });
        }

        // Check for pending approvals
        var pendingCount = await _context.Investments
            .CountAsync(i => i.Status == InvestmentStatus.Pending);

        if (pendingCount > 5)
        {
            alerts.Add(new InvestmentAlertDto
            {
                Type = "Approval",
                Severity = "Medium",
                Message = $"{pendingCount} investments awaiting approval",
                Timestamp = DateTime.UtcNow
            });
        }

        // Check for maturing investments
        var maturingSoon = await _context.Investments
            .Where(i => i.Status == InvestmentStatus.Active &&
                       i.MaturityDate <= DateTime.UtcNow.AddDays(30))
            .CountAsync();

        if (maturingSoon > 0)
        {
            alerts.Add(new InvestmentAlertDto
            {
                Type = "Maturity",
                Severity = "Low",
                Message = $"{maturingSoon} investments maturing in the next 30 days",
                Timestamp = DateTime.UtcNow
            });
        }

        return alerts;
    }

    // Helper Methods
    private InvestmentDto MapToDto(Investment investment)
    {
        return new InvestmentDto
        {
            Id = investment.Id,
            PlanName = investment.Plan?.Name ?? "Unknown",
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
            NextPayoutAmount = CalculatePayoutAmount(investment),
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
            TotalInvested = plan.Investments?.Where(i => i.Status == InvestmentStatus.Active)
                .Sum(i => i.Amount) ?? 0
        };
    }

    private decimal CalculateProjectedReturn(decimal amount, decimal annualROI, int termMonths)
    {
        decimal monthlyRate = annualROI / 12 / 100;
        decimal termYears = termMonths / 12m;
        return amount * (1 + (annualROI / 100 * termYears));
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
            PayoutFrequency.AtMaturity => investment.MaturityDate,
            _ => null
        };
    }

    private decimal CalculatePayoutAmount(Investment investment)
    {
        var monthlyRate = investment.CustomROI / 12 / 100;

        return investment.PayoutFrequency switch
        {
            PayoutFrequency.Monthly => investment.Amount * monthlyRate,
            PayoutFrequency.Quarterly => investment.Amount * monthlyRate * 3,
            PayoutFrequency.SemiAnnually => investment.Amount * monthlyRate * 6,
            PayoutFrequency.Annually => investment.Amount * (investment.CustomROI / 100),
            PayoutFrequency.AtMaturity => investment.ProjectedReturn - investment.Amount,
            _ => 0
        };
    }

    private async Task ProcessPayout(Investment investment)
    {
        var payoutAmount = CalculatePayoutAmount(investment);

        // Create payout record
        var payout = new InvestmentReturn
        {
            Id = Guid.NewGuid(),
            InvestmentId = investment.Id,
            Amount = payoutAmount,
            Currency = investment.Currency,
            InterestAmount = payoutAmount,
            PrincipalAmount = 0,
            Type = ReturnType.Interest,
            PaymentDate = DateTime.UtcNow,
            ProcessedDate = DateTime.UtcNow,
            Status = PaymentStatus.Completed,
            Description = $"{investment.PayoutFrequency} payout",
            CreatedAt = DateTime.UtcNow
        };

        _context.InvestmentReturns.Add(payout);

        // Update investment
        investment.TotalPaidOut += payoutAmount;
        investment.LastPayoutDate = DateTime.UtcNow;

        // Credit user account
        var userAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == investment.UserId &&
                                    a.Currency == investment.Currency &&
                                    a.IsPriority);

        if (userAccount != null)
        {
            userAccount.Balance += payoutAmount;
            userAccount.UpdatedAt = DateTime.UtcNow;
        }
    }

    private string DetermineRiskProfile(List<Investment> investments)
    {
        if (!investments.Any())
            return "Unknown";

        var avgRisk = investments
            .Where(i => i.Plan != null)
            .Select(i => (int)i.Plan.RiskLevel)
            .DefaultIfEmpty(2)
            .Average();

        return avgRisk switch
        {
            < 1 => "Conservative",
            < 2 => "Moderate",
            < 3 => "Balanced",
            < 4 => "Growth",
            _ => "Aggressive"
        };
    }

    private async Task<List<UserInvestmentOverviewDto>> GetTopInvestorsAsync(int count)
    {
        return await _context.Investments
            .Include(i => i.User)
            .Where(i => i.Status == InvestmentStatus.Active)
            .GroupBy(i => i.User)
            .Select(g => new UserInvestmentOverviewDto
            {
                UserId = g.Key.Id,
                UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                UserEmail = g.Key.Email,
                TotalInvestments = g.Count(),
                TotalInvested = g.Sum(i => i.Amount),
                CurrentValue = g.Sum(i => i.Amount + (i.TotalPaidOut * 0.8m)),
                AverageROI = g.Average(i => i.CustomROI),
                TotalReturns = g.Sum(i => i.TotalPaidOut)
            })
            .OrderByDescending(i => i.TotalInvested)
            .Take(count)
            .ToListAsync();
    }
}