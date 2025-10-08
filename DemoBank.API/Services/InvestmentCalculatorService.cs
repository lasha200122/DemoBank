using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class InvestmentCalculatorService : IInvestmentCalculatorService
{
    private readonly DemoBankContext _context;
    private readonly ILogger<InvestmentCalculatorService> _logger;

    // Constants for calculations
    private const decimal INFLATION_RATE = 2.5m; // Annual inflation rate
    private const decimal SAVINGS_ACCOUNT_RATE = 2.0m; // Typical savings account rate
    private const decimal STOCK_MARKET_AVERAGE = 10.0m; // Historical S&P 500 average

    public InvestmentCalculatorService(
        DemoBankContext context,
        ILogger<InvestmentCalculatorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InvestmentCalculatorResultDto> CalculateReturnsAsync(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency,
        string currency = "USD")
    {
        return await Task.Run(() =>
        {
            var result = new InvestmentCalculatorResultDto
            {
                InitialAmount = amount,
                Currency = currency,
                TermMonths = termMonths,
                AnnualROI = annualROI,
                PayoutFrequency = frequency.ToString()
            };

            // Calculate monthly interest rate
            decimal monthlyRate = annualROI / 12 / 100;
            decimal quarterlyRate = annualROI / 4 / 100;
            decimal semiAnnualRate = annualROI / 2 / 100;
            decimal annualRate = annualROI / 100;

            // Calculate payouts based on frequency
            switch (frequency)
            {
                case PayoutFrequency.Monthly:
                    result.MonthlyPayout = amount * monthlyRate;
                    result.TotalInterest = result.MonthlyPayout * termMonths;
                    result.EffectiveROI = CalculateCompoundedRate(annualROI, 12);
                    break;

                case PayoutFrequency.Quarterly:
                    var quarters = termMonths / 3;
                    result.QuarterlyPayout = amount * quarterlyRate;
                    result.MonthlyPayout = result.QuarterlyPayout / 3;
                    result.TotalInterest = result.QuarterlyPayout * quarters;
                    result.EffectiveROI = CalculateCompoundedRate(annualROI, 4);
                    break;

                case PayoutFrequency.SemiAnnually:
                    var semiAnnuals = termMonths / 6;
                    var semiAnnualPayout = amount * semiAnnualRate;
                    result.MonthlyPayout = semiAnnualPayout / 6;
                    result.TotalInterest = semiAnnualPayout * semiAnnuals;
                    result.EffectiveROI = CalculateCompoundedRate(annualROI, 2);
                    break;

                case PayoutFrequency.Annually:
                    var years = termMonths / 12;
                    result.AnnualPayout = amount * annualRate;
                    result.MonthlyPayout = result.AnnualPayout / 12;
                    result.TotalInterest = result.AnnualPayout * years;
                    result.EffectiveROI = annualROI;
                    break;

                case PayoutFrequency.AtMaturity:
                    // Compound interest calculation
                    decimal termYears = termMonths / 12m;
                    result.FinalValue = amount * (decimal)Math.Pow((double)(1 + annualRate), (double)termYears);
                    result.TotalInterest = result.FinalValue - amount;
                    result.MonthlyPayout = 0;
                    result.EffectiveROI = annualROI;
                    break;
            }

            // Calculate totals
            result.TotalPayout = amount + result.TotalInterest;
            result.FinalValue = result.FinalValue == 0 ? result.TotalPayout : result.FinalValue;

            // Generate payout schedule
            result.PayoutSchedule = GeneratePayoutSchedule(amount, termMonths, annualROI, frequency);

            // Generate yearly breakdown
            result.YearlyBreakdown = GenerateYearlyBreakdown(amount, termMonths, annualROI, frequency);

            // Generate comparison
            result.Comparison = GenerateComparison(amount, result.TotalInterest, termMonths).Result;

            return result;
        });
    }

    private async Task<ComparisonDto> GenerateComparison(
        decimal amount,
        decimal totalInterest,
        int termMonths)
    {
        return await Task.Run(() =>
        {
            decimal termYears = termMonths / 12m;

            // Typical benchmark rates (you can make these configurable)
            decimal savingsAccountRate = 0.015m; // 1.5% typical savings account
            decimal inflationRate = 0.025m;      // 2.5% typical inflation
            decimal stockMarketRate = 0.10m;     // 10% historical stock market average

            // Calculate what would have been earned with each alternative
            decimal savingsInterest = amount * (decimal)Math.Pow((double)(1 + savingsAccountRate), (double)termYears) - amount;
            decimal inflationLoss = amount * (decimal)Math.Pow((double)(1 + inflationRate), (double)termYears) - amount;
            decimal stockMarketGain = amount * (decimal)Math.Pow((double)(1 + stockMarketRate), (double)termYears) - amount;

            return new ComparisonDto
            {
                // Positive = you earned more, Negative = you earned less
                VsSavingsAccount = totalInterest - savingsInterest,
                VsInflation = totalInterest - inflationLoss,
                VsStockMarket = totalInterest - stockMarketGain
            };
        });
    }

    public async Task<Dictionary<string, InvestmentCalculatorResultDto>> CompareInvestmentPlansAsync(
        decimal amount,
        int termMonths,
        List<Guid> planIds)
    {
        var results = new Dictionary<string, InvestmentCalculatorResultDto>();

        var plans = await _context.InvestmentPlans
            .Where(p => planIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        foreach (var plan in plans)
        {
            var result = await CalculateReturnsAsync(
                amount,
                termMonths,
                plan.BaseROI,
                plan.DefaultPayoutFrequency,
                plan.Currency
            );

            results[plan.Name] = result;
        }

        return results;
    }

    public async Task<PayoutScheduleDto> GeneratePayoutScheduleAsync(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency,
        DateTime startDate)
    {
        return await Task.Run(() => GeneratePayoutSchedule(amount, termMonths, annualROI, frequency, startDate).First());
    }

    public async Task<decimal> CalculateEarlyWithdrawalPenaltyAsync(
        Guid investmentId,
        decimal withdrawalAmount)
    {
        var investment = await _context.Investments
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException("Investment not found");

        // Calculate percentage of term completed
        var totalDays = (investment.MaturityDate - investment.StartDate).TotalDays;
        var completedDays = (DateTime.UtcNow - investment.StartDate).TotalDays;
        var completionPercentage = (decimal)(completedDays / totalDays) * 100;

        // Base penalty from plan
        var basePenalty = investment.Plan?.EarlyWithdrawalPenalty ?? 5.0m;

        // Reduce penalty based on completion percentage
        var adjustedPenalty = basePenalty * (1 - (completionPercentage / 100));

        // Calculate penalty amount
        var penaltyAmount = withdrawalAmount * (adjustedPenalty / 100);

        // Add loss of interest penalty
        var expectedInterest = investment.ProjectedReturn - investment.Amount;
        var earnedInterest = investment.TotalPaidOut;
        var lostInterest = Math.Max(0, (expectedInterest - earnedInterest) * 0.5m);

        return penaltyAmount + lostInterest;
    }

    public async Task<ComparisonDto> GenerateComparisonAsync(
        decimal amount,
        decimal returns,
        int termMonths)
    {
        return await Task.Run(() =>
        {
            var years = termMonths / 12m;

            // Calculate alternative returns
            var savingsReturn = amount * (SAVINGS_ACCOUNT_RATE / 100) * years;
            var inflationLoss = amount * (INFLATION_RATE / 100) * years;
            var stockMarketReturn = amount * (decimal)Math.Pow((double)(1 + STOCK_MARKET_AVERAGE / 100), (double)years) - amount;

            return new ComparisonDto
            {
                VsSavingsAccount = returns - savingsReturn,
                VsInflation = returns - inflationLoss,
                VsStockMarket = returns - stockMarketReturn
            };
        });
    }

    private decimal CalculateCompoundedRate(decimal nominalRate, int compoundingPeriods)
    {
        // Calculate effective annual rate with compounding
        decimal periodicRate = nominalRate / compoundingPeriods / 100;
        decimal effectiveRate = (decimal)Math.Pow((double)(1 + periodicRate), compoundingPeriods) - 1;
        return effectiveRate * 100;
    }

    private List<PayoutScheduleDto> GeneratePayoutSchedule(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency,
        DateTime? startDate = null)
    {
        var schedule = new List<PayoutScheduleDto>();
        var currentDate = startDate ?? DateTime.UtcNow;
        decimal balance = amount;
        decimal monthlyRate = annualROI / 12 / 100;

        int periods = frequency switch
        {
            PayoutFrequency.Monthly => termMonths,
            PayoutFrequency.Quarterly => termMonths / 3,
            PayoutFrequency.SemiAnnually => termMonths / 6,
            PayoutFrequency.Annually => termMonths / 12,
            PayoutFrequency.AtMaturity => 1,
            _ => termMonths
        };

        int monthsPerPeriod = frequency switch
        {
            PayoutFrequency.Monthly => 1,
            PayoutFrequency.Quarterly => 3,
            PayoutFrequency.SemiAnnually => 6,
            PayoutFrequency.Annually => 12,
            PayoutFrequency.AtMaturity => termMonths,
            _ => 1
        };

        for (int i = 1; i <= periods; i++)
        {
            currentDate = currentDate.AddMonths(monthsPerPeriod);
            decimal periodRate = monthlyRate * monthsPerPeriod;
            decimal interest = balance * periodRate;

            var payout = new PayoutScheduleDto
            {
                Period = i,
                Date = currentDate,
                Principal = i == periods && frequency == PayoutFrequency.AtMaturity ? amount : 0,
                Interest = interest,
                TotalPayout = interest + (i == periods && frequency == PayoutFrequency.AtMaturity ? amount : 0),
                Balance = balance
            };

            schedule.Add(payout);

            if (frequency == PayoutFrequency.AtMaturity)
            {
                // For at-maturity, calculate compound interest
                decimal years = termMonths / 12m;
                payout.Interest = amount * (decimal)Math.Pow((double)(1 + annualROI / 100), (double)years) - amount;
                payout.TotalPayout = amount + payout.Interest;
            }
        }

        return schedule;
    }

    private Dictionary<string, decimal> GenerateYearlyBreakdown(
        decimal amount,
        int termMonths,
        decimal annualROI,
        PayoutFrequency frequency)
    {
        var breakdown = new Dictionary<string, decimal>();
        var years = (int)Math.Ceiling(termMonths / 12m);
        decimal annualReturn = amount * (annualROI / 100);

        for (int year = 1; year <= years; year++)
        {
            var monthsInYear = Math.Min(12, termMonths - (year - 1) * 12);
            var yearReturn = annualReturn * (monthsInYear / 12m);

            breakdown[$"Year {year}"] = yearReturn;
        }

        breakdown["Total"] = breakdown.Values.Sum();

        return breakdown;
    }
}