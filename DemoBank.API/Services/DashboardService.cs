using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class DashboardService : IDashboardService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICurrencyService _currencyService;
    private readonly ILoanService _loanService;
    private readonly IInvoiceService _invoiceService;

    public DashboardService(
        DemoBankContext context,
        IAccountService accountService,
        ITransactionService transactionService,
        ICurrencyService currencyService,
        ILoanService loanService,
        IInvoiceService invoiceService)
    {
        _context = context;
        _accountService = accountService;
        _transactionService = transactionService;
        _currencyService = currencyService;
        _loanService = loanService;
        _invoiceService = invoiceService;
    }

    public async Task<EnhancedDashboardDto> GetEnhancedDashboardAsync(Guid userId)
    {
        var user = await _context.Users
         .Include(u => u.Settings)
         .Include(u => u.Accounts)
         .Include(u => u.Loans)
         .Include(u => u.BankingDetails)
         .AsNoTracking()
         .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found");

        var clientInvestments = await _context.ClientInvestment
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        var activeAccounts = user.Accounts.Where(a => a.IsActive).ToList();

        // Calculate monthly and yearly returns
        var monthlyReturnsUSD = activeAccounts
            .Where(a => a.Currency == "USD")
            .Join(clientInvestments,
                  a => a.Id.ToString(),
                  ci => ci.AccountId,
                  (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
            .Sum();

        var yearlyReturnsUSD = activeAccounts
            .Where(a => a.Currency == "USD")
            .Join(clientInvestments,
                  a => a.Id.ToString(),
                  ci => ci.AccountId,
                  (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
            .Sum();

        var monthlyReturnsEUR = activeAccounts
            .Where(a => a.Currency == "EUR")
            .Join(clientInvestments,
                  a => a.Id.ToString(),
                  ci => ci.AccountId,
                  (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
            .Sum();

        var yearlyReturnsEUR = activeAccounts
            .Where(a => a.Currency == "EUR")
            .Join(clientInvestments,
                  a => a.Id.ToString(),
                  ci => ci.AccountId,
                  (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
            .Sum();

        // Get all the necessary data
        var accounts = await _accountService.GetActiveUserAccountsAsync(userId);
        var totalBalanceUSD = await _accountService.GetTotalBalanceInUSDAsync(userId);
        var balancesByCurrency = await _accountService.GetBalancesByCurrencyAsync(userId);

        // Get transaction statistics for the last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var transactionSummary = await _transactionService.GetTransactionSummaryAsync(
            userId, thirtyDaysAgo, DateTime.UtcNow);

        // Get recent transactions
        var recentTransactions = await _transactionService.GetUserTransactionsAsync(userId, 10);

        // Get pending items
        var pendingInvoices = await _invoiceService.GetPendingInvoicesAsync(userId);
        var loans = await _loanService.GetUserLoansAsync(userId);
        var activeLoans = loans.Where(l => l.Status == "Active").ToList();

        // Calculate trends
        var previousMonth = DateTime.UtcNow.AddMonths(-1);
        var previousMonthSummary = await _transactionService.GetTransactionSummaryAsync(
            userId, previousMonth.AddDays(-30), previousMonth);

        decimal balanceTrend = 0;
        if (previousMonthSummary.TotalDepositsUSD > 0)
        {
            balanceTrend = ((transactionSummary.TotalDepositsUSD - previousMonthSummary.TotalDepositsUSD)
                / previousMonthSummary.TotalDepositsUSD) * 100;
        }

        // Get notifications
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Get spending categories
        var spendingByCategory = await GetSpendingByCategoryAsync(userId, thirtyDaysAgo, DateTime.UtcNow);

        // Calculate financial score
        var financialScore = CalculateFinancialScore(
            totalBalanceUSD,
            transactionSummary,
            activeLoans.Count,
            accounts.Count);

        return new EnhancedDashboardDto
        {
            UserInfo = new UserInfoDto
            {
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                MemberSince = user.CreatedAt,
                LastLogin = DateTime.UtcNow,
                PreferredCurrency = user.Settings?.PreferredCurrency ?? "USD"
            },
            AccountsSummary = new EnhancedAccountSummaryDto
            {
                TotalAccounts = accounts.Count,
                ActiveAccounts = accounts.Count(a => a.IsActive),
                TotalBalanceUSD = totalBalanceUSD,
                BalancesByCurrency = balancesByCurrency,
                BalanceTrend = balanceTrend,
                PrimaryAccount = accounts.FirstOrDefault(a => a.IsPriority)?.AccountNumber,
                MonthlyReturnsEUR = monthlyReturnsEUR,
                MonthlyReturnsUSD = monthlyReturnsUSD,
                YearlyReturnsEUR = yearlyReturnsEUR,
                YearlyReturnsUSD = yearlyReturnsUSD
            },
            TransactionMetrics = new TransactionMetricsDto
            {
                Last30Days = new PeriodMetricsDto
                {
                    TotalTransactions = transactionSummary.TotalTransactions,
                    TotalIncome = transactionSummary.TotalDepositsUSD,
                    TotalExpenses = transactionSummary.TotalWithdrawalsUSD + transactionSummary.TotalTransfersUSD,
                    NetCashFlow = transactionSummary.TotalDepositsUSD -
                        (transactionSummary.TotalWithdrawalsUSD + transactionSummary.TotalTransfersUSD),
                    AverageTransactionSize = transactionSummary.TotalTransactions > 0 ?
                        (transactionSummary.TotalDepositsUSD + transactionSummary.TotalWithdrawalsUSD +
                         transactionSummary.TotalTransfersUSD) / transactionSummary.TotalTransactions : 0
                }
            },
            PendingItems = new PendingItemsDto
            {
                PendingInvoices = pendingInvoices.Count,
                PendingInvoicesAmount = pendingInvoices.Sum(i => i.Amount),
                ActiveLoans = activeLoans.Count,
                NextLoanPayment = activeLoans
                    .Where(l => l.NextPaymentDate != null)
                    .Min(l => l.NextPaymentDate),
                TotalLoanBalance = activeLoans.Sum(l => l.RemainingBalance)
            },
            SpendingByCategory = spendingByCategory,
            RecentActivity = recentTransactions.Select(t => new ActivityItemDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Description = t.Description,
                Amount = t.Amount,
                Currency = t.Currency,
                Timestamp = t.CreatedAt,
                Status = t.Status.ToString()
            }).ToList(),
            UnreadNotifications = unreadNotifications.Count,
            FinancialScore = financialScore,
            Insights = GenerateInsights(transactionSummary, totalBalanceUSD, activeLoans)
        };
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(Guid userId, int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var analytics = new AnalyticsDto
        {
            Period = $"Last {months} months",
            MonthlyData = new List<MonthlyAnalyticsDto>(),
            TotalIncome = 0,
            TotalExpenses = 0,
            AverageMonthlyIncome = 0,
            AverageMonthlyExpenses = 0
        };

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-i).Date.AddDays(1 - DateTime.UtcNow.AddMonths(-i).Day);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthlyStats = await _transactionService.GetMonthlyStatisticsAsync(
                userId, monthStart.Year, monthStart.Month);

            var monthlyData = new MonthlyAnalyticsDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Income = monthlyStats.GetValueOrDefault("TotalDeposits", 0),
                Expenses = monthlyStats.GetValueOrDefault("TotalWithdrawals", 0) +
                          monthlyStats.GetValueOrDefault("TotalTransfers", 0),
                NetCashFlow = monthlyStats.GetValueOrDefault("NetCashFlow", 0),
                TransactionCount = await _context.Transactions
                    .Include(t => t.Account)
                    .CountAsync(t => t.Account.UserId == userId &&
                                   t.CreatedAt >= monthStart &&
                                   t.CreatedAt <= monthEnd)
            };

            analytics.MonthlyData.Add(monthlyData);
            analytics.TotalIncome += monthlyData.Income;
            analytics.TotalExpenses += monthlyData.Expenses;
        }

        analytics.AverageMonthlyIncome = analytics.TotalIncome / months;
        analytics.AverageMonthlyExpenses = analytics.TotalExpenses / months;
        analytics.SavingsRate = analytics.TotalIncome > 0 ?
            ((analytics.TotalIncome - analytics.TotalExpenses) / analytics.TotalIncome) * 100 : 0;

        // Calculate trends
        if (analytics.MonthlyData.Count >= 2)
        {
            var lastMonth = analytics.MonthlyData[analytics.MonthlyData.Count - 1];
            var previousMonth = analytics.MonthlyData[analytics.MonthlyData.Count - 2];

            analytics.IncomeTrend = previousMonth.Income > 0 ?
                ((lastMonth.Income - previousMonth.Income) / previousMonth.Income) * 100 : 0;

            analytics.ExpenseTrend = previousMonth.Expenses > 0 ?
                ((lastMonth.Expenses - previousMonth.Expenses) / previousMonth.Expenses) * 100 : 0;
        }

        return analytics;
    }

    public async Task<ActivityFeedDto> GetActivityFeedAsync(Guid userId, int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var activities = new List<ActivityFeedItemDto>();

        // Get transactions
        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .Where(t => (t.Account.UserId == userId || t.ToAccount.UserId == userId) &&
                       t.CreatedAt >= startDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            activities.Add(new ActivityFeedItemDto
            {
                Id = transaction.Id,
                Type = "Transaction",
                Category = transaction.Type.ToString(),
                Title = GetTransactionTitle(transaction),
                Description = transaction.Description,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Icon = GetActivityIcon(transaction.Type.ToString()),
                Timestamp = transaction.CreatedAt,
                Status = transaction.Status.ToString()
            });
        }

        // Get loan activities
        var loanPayments = await _context.LoanPayments
            .Include(p => p.Loan)
            .Where(p => p.Loan.UserId == userId && p.PaymentDate >= startDate)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        foreach (var payment in loanPayments)
        {
            activities.Add(new ActivityFeedItemDto
            {
                Id = payment.Id,
                Type = "LoanPayment",
                Category = "Loan",
                Title = "Loan Payment",
                Description = $"Payment for loan #{payment.LoanId.ToString().Substring(0, 8)}",
                Amount = payment.Amount,
                Currency = "USD",
                Icon = "loan",
                Timestamp = payment.PaymentDate,
                Status = "Completed"
            });
        }

        // Get invoice activities
        var invoices = await _context.Invoices
            .Where(i => i.UserId == userId &&
                       (i.CreatedAt >= startDate || i.PaidDate >= startDate))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        foreach (var invoice in invoices)
        {
            activities.Add(new ActivityFeedItemDto
            {
                Id = invoice.Id,
                Type = "Invoice",
                Category = invoice.Status.ToString(),
                Title = invoice.PaidDate.HasValue ? "Invoice Paid" : "Invoice Created",
                Description = invoice.Description,
                Amount = invoice.Amount,
                Currency = invoice.Currency,
                Icon = "invoice",
                Timestamp = invoice.PaidDate ?? invoice.CreatedAt,
                Status = invoice.Status.ToString()
            });
        }

        // Sort all activities by timestamp
        activities = activities.OrderByDescending(a => a.Timestamp).ToList();

        // Group by date
        var groupedActivities = activities
            .GroupBy(a => a.Timestamp.Date)
            .Select(g => new DailyActivityDto
            {
                Date = g.Key,
                Activities = g.ToList(),
                TotalAmount = g.Sum(a => a.Amount ?? 0)
            })
            .ToList();

        return new ActivityFeedDto
        {
            Period = $"Last {days} days",
            TotalActivities = activities.Count,
            DailyActivities = groupedActivities
        };
    }

    public async Task<QuickActionsDto> GetQuickActionsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Accounts)
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var frequentRecipients = await GetFrequentRecipientsAsync(userId);
        var favoriteCurrencyPairs = await GetFavoriteCurrencyPairsAsync(userId);

        return new QuickActionsDto
        {
            AvailableActions = new List<QuickActionDto>
            {
                new QuickActionDto
                {
                    Id = "transfer",
                    Title = "Quick Transfer",
                    Icon = "send",
                    Enabled = user.Accounts.Any(a => a.IsActive && a.Balance > 0)
                },
                new QuickActionDto
                {
                    Id = "exchange",
                    Title = "Exchange Currency",
                    Icon = "exchange",
                    Enabled = user.Accounts.Count(a => a.IsActive) > 1
                },
                new QuickActionDto
                {
                    Id = "pay_invoice",
                    Title = "Pay Invoice",
                    Icon = "invoice",
                    Enabled = true
                },
                new QuickActionDto
                {
                    Id = "loan_payment",
                    Title = "Make Loan Payment",
                    Icon = "payment",
                    Enabled = await _context.Loans.AnyAsync(l =>
                        l.UserId == userId && l.Status == LoanStatus.Active)
                }
            },
            FrequentRecipients = frequentRecipients,
            FavoriteCurrencyPairs = favoriteCurrencyPairs,
            SuggestedActions = await GetSuggestedActionsAsync(userId)
        };
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync()
    {
        var systemStatus = new SystemStatusDto
        {
            Status = "Operational",
            LastUpdated = DateTime.UtcNow,
            Services = new List<ServiceStatusDto>
            {
                new ServiceStatusDto
                {
                    Name = "Banking Services",
                    Status = "Operational",
                    ResponseTime = 45,
                    Uptime = 99.99m
                },
                new ServiceStatusDto
                {
                    Name = "Payment Processing",
                    Status = "Operational",
                    ResponseTime = 120,
                    Uptime = 99.95m
                },
                new ServiceStatusDto
                {
                    Name = "Currency Exchange",
                    Status = "Operational",
                    ResponseTime = 85,
                    Uptime = 99.97m
                },
                new ServiceStatusDto
                {
                    Name = "Loan Services",
                    Status = "Operational",
                    ResponseTime = 95,
                    Uptime = 99.98m
                }
            }
        };

        // Calculate overall metrics
        systemStatus.TotalUsers = await _context.Users.CountAsync();
        systemStatus.ActiveSessions = Random.Shared.Next(100, 500); // Simulated
        systemStatus.TransactionsToday = await _context.Transactions
            .CountAsync(t => t.CreatedAt.Date == DateTime.UtcNow.Date);

        systemStatus.SystemLoad = Random.Shared.Next(20, 60); // Simulated percentage

        return systemStatus;
    }

    public async Task<FinancialHealthDto> GetFinancialHealthAsync(Guid userId)
    {
        var accounts = await _accountService.GetActiveUserAccountsAsync(userId);
        var totalBalance = await _accountService.GetTotalBalanceInUSDAsync(userId);

        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var transactionSummary = await _transactionService.GetTransactionSummaryAsync(
            userId, threeMonthsAgo, DateTime.UtcNow);

        var loans = await _loanService.GetUserLoansAsync(userId);
        var activeLoans = loans.Where(l => l.Status == "Active").ToList();

        // Calculate metrics
        var monthlyIncome = transactionSummary.TotalDepositsUSD / 3;
        var monthlyExpenses = (transactionSummary.TotalWithdrawalsUSD +
                              transactionSummary.TotalTransfersUSD) / 3;
        var savingsRate = monthlyIncome > 0 ?
            ((monthlyIncome - monthlyExpenses) / monthlyIncome) * 100 : 0;

        var debtToIncomeRatio = monthlyIncome > 0 ?
            (activeLoans.Sum(l => l.MonthlyPayment) / monthlyIncome) * 100 : 0;

        var emergencyFundMonths = monthlyExpenses > 0 ?
            totalBalance / monthlyExpenses : 0;

        var healthScore = CalculateHealthScore(
            savingsRate, debtToIncomeRatio, emergencyFundMonths, accounts.Count);

        return new FinancialHealthDto
        {
            HealthScore = healthScore,
            ScoreCategory = GetScoreCategory(healthScore),
            Metrics = new HealthMetricsDto
            {
                SavingsRate = savingsRate,
                DebtToIncomeRatio = debtToIncomeRatio,
                EmergencyFundMonths = emergencyFundMonths,
                CreditUtilization = Random.Shared.Next(10, 40), // Simulated
                NetWorth = totalBalance - activeLoans.Sum(l => l.RemainingBalance),
                MonthlyNetIncome = monthlyIncome - monthlyExpenses
            },
            Recommendations = GenerateHealthRecommendations(
                savingsRate, debtToIncomeRatio, emergencyFundMonths),
            Trends = new HealthTrendsDto
            {
                ScoreTrend = Random.Shared.Next(-5, 10), // Simulated
                SavingsTrend = Random.Shared.Next(-10, 20), // Simulated
                DebtTrend = activeLoans.Any() ? -5 : 0
            }
        };
    }

    public async Task<SpendingAnalysisDto> GetSpendingAnalysisAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Withdrawal &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .ToListAsync();

        var categories = new Dictionary<string, decimal>();
        var merchants = new Dictionary<string, decimal>();

        // Simulate categorization (in real app, this would be based on merchant data)
        var categoryList = new[] { "Food & Dining", "Transportation", "Shopping",
                                   "Entertainment", "Bills & Utilities", "Healthcare", "Other" };

        foreach (var transaction in transactions)
        {
            var category = categoryList[Random.Shared.Next(categoryList.Length)];

            if (!categories.ContainsKey(category))
                categories[category] = 0;

            var amountUSD = transaction.Currency == "USD" ? transaction.Amount :
                await _currencyService.ConvertCurrencyAsync(
                    transaction.Amount, transaction.Currency, "USD");

            categories[category] += amountUSD;

            // Simulate merchant data
            var merchant = $"Merchant_{Random.Shared.Next(1, 20)}";
            if (!merchants.ContainsKey(merchant))
                merchants[merchant] = 0;
            merchants[merchant] += amountUSD;
        }

        var totalSpending = categories.Values.Sum();

        return new SpendingAnalysisDto
        {
            Period = $"{startDate:MMM dd} - {endDate:MMM dd}",
            TotalSpending = totalSpending,
            Categories = categories.Select(c => new CategorySpendingDto
            {
                Category = c.Key,
                Amount = c.Value,
                Percentage = totalSpending > 0 ? (c.Value / totalSpending) * 100 : 0,
                TransactionCount = Random.Shared.Next(5, 50) // Simulated
            }).OrderByDescending(c => c.Amount).ToList(),
            TopMerchants = merchants.OrderByDescending(m => m.Value)
                .Take(5)
                .Select(m => new MerchantSpendingDto
                {
                    Merchant = m.Key,
                    Amount = m.Value,
                    TransactionCount = Random.Shared.Next(1, 10) // Simulated
                }).ToList(),
            DailyAverage = totalSpending / Math.Max(1, (endDate - startDate).Days),
            HighestSpendingDay = transactions.Any() ?
                transactions.GroupBy(t => t.CreatedAt.Date)
                    .OrderByDescending(g => g.Sum(t => t.Amount))
                    .First().Key : (DateTime?)null
        };
    }

    public async Task<GoalsProgressDto> GetGoalsProgressAsync(Guid userId)
    {
        // Simulate financial goals (in real app, these would be stored in database)
        var goals = new List<GoalDto>
        {
            new GoalDto
            {
                Id = Guid.NewGuid(),
                Name = "Emergency Fund",
                TargetAmount = 10000,
                CurrentAmount = await _accountService.GetTotalBalanceInUSDAsync(userId) * 0.3m,
                TargetDate = DateTime.UtcNow.AddMonths(6),
                Category = "Savings"
            },
            new GoalDto
            {
                Id = Guid.NewGuid(),
                Name = "Vacation Fund",
                TargetAmount = 5000,
                CurrentAmount = Random.Shared.Next(500, 3000),
                TargetDate = DateTime.UtcNow.AddMonths(12),
                Category = "Savings"
            },
            new GoalDto
            {
                Id = Guid.NewGuid(),
                Name = "Debt Reduction",
                TargetAmount = 0,
                CurrentAmount = (await _loanService.GetUserLoansAsync(userId))
                    .Where(l => l.Status == "Active")
                    .Sum(l => l.RemainingBalance),
                TargetDate = DateTime.UtcNow.AddYears(2),
                Category = "Debt"
            }
        };

        foreach (var goal in goals)
        {
            goal.Progress = goal.Category == "Debt" ?
                (goal.TargetAmount == 0 && goal.CurrentAmount > 0 ?
                    Math.Max(0, 100 - (goal.CurrentAmount / 10000m * 100)) : 100) :
                (goal.TargetAmount > 0 ?
                    Math.Min(100, (goal.CurrentAmount / goal.TargetAmount) * 100) : 0);

            goal.MonthlyContribution = goal.TargetDate > DateTime.UtcNow ?
                (goal.TargetAmount - goal.CurrentAmount) /
                Math.Max(1, (goal.TargetDate - DateTime.UtcNow).Days / 30) : 0;

            goal.Status = goal.Progress >= 100 ? "Completed" :
                         goal.Progress >= 75 ? "On Track" :
                         goal.Progress >= 50 ? "Behind" : "Just Started";
        }

        return new GoalsProgressDto
        {
            Goals = goals,
            TotalGoals = goals.Count,
            CompletedGoals = goals.Count(g => g.Status == "Completed"),
            AverageProgress = goals.Average(g => g.Progress)
        };
    }

    // Helper methods
    private async Task<Dictionary<string, decimal>> GetSpendingByCategoryAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        var categories = new Dictionary<string, decimal>
        {
            { "Food & Dining", 0 },
            { "Transportation", 0 },
            { "Shopping", 0 },
            { "Entertainment", 0 },
            { "Bills & Utilities", 0 },
            { "Healthcare", 0 },
            { "Other", 0 }
        };

        var transactions = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       (t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Transfer) &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            // Simulate categorization
            var categoryList = categories.Keys.ToList();
            var category = categoryList[Random.Shared.Next(categoryList.Count)];

            var amountUSD = transaction.Currency == "USD" ? transaction.Amount :
                await _currencyService.ConvertCurrencyAsync(
                    transaction.Amount, transaction.Currency, "USD");

            categories[category] += amountUSD;
        }

        return categories;
    }

    private int CalculateFinancialScore(
        decimal totalBalance, TransactionSummaryDto transactions,
        int activeLoans, int accountCount)
    {
        var score = 500; // Base score

        // Balance factor (up to 200 points)
        if (totalBalance >= 10000) score += 200;
        else if (totalBalance >= 5000) score += 150;
        else if (totalBalance >= 1000) score += 100;
        else if (totalBalance >= 500) score += 50;

        // Cash flow factor (up to 150 points)
        var netCashFlow = transactions.TotalDepositsUSD -
            (transactions.TotalWithdrawalsUSD + transactions.TotalTransfersUSD);
        if (netCashFlow > 0) score += Math.Min(150, (int)(netCashFlow / 100));

        // Loan factor (up to 100 points)
        score -= activeLoans * 25;

        // Account diversity (up to 50 points)
        score += Math.Min(50, accountCount * 10);

        return Math.Max(300, Math.Min(850, score));
    }

    private int CalculateHealthScore(
        decimal savingsRate, decimal debtToIncomeRatio,
        decimal emergencyFundMonths, int accountCount)
    {
        var score = 50; // Base score

        // Savings rate (up to 30 points)
        score += (int)Math.Min(30, savingsRate);

        // Debt ratio (up to 20 points, inverse)
        score += Math.Max(0, 20 - (int)(debtToIncomeRatio / 2));

        // Emergency fund (up to 30 points)
        score += (int)Math.Min(30, emergencyFundMonths * 5);

        // Account diversity (up to 20 points)
        score += Math.Min(20, accountCount * 5);

        return Math.Max(0, Math.Min(100, score));
    }

    private string GetScoreCategory(int score)
    {
        if (score >= 80) return "Excellent";
        if (score >= 60) return "Good";
        if (score >= 40) return "Fair";
        if (score >= 20) return "Needs Improvement";
        return "Poor";
    }

    private List<string> GenerateInsights(
        TransactionSummaryDto transactions, decimal balance, List<LoanDto> loans)
    {
        var insights = new List<string>();

        // Cash flow insight
        var netCashFlow = transactions.TotalDepositsUSD -
            (transactions.TotalWithdrawalsUSD + transactions.TotalTransfersUSD);
        if (netCashFlow > 0)
            insights.Add($"Great job! You saved ${netCashFlow:N2} in the last 30 days.");
        else if (netCashFlow < 0)
            insights.Add($"You spent ${Math.Abs(netCashFlow):N2} more than you earned last month.");

        // Balance insight
        if (balance < 1000)
            insights.Add("Consider building your emergency fund to at least $1,000.");

        // Loan insight
        if (loans.Any())
        {
            var nextPayment = loans.Min(l => l.NextPaymentDate);
            if (nextPayment.HasValue && nextPayment.Value < DateTime.UtcNow.AddDays(7))
                insights.Add($"Loan payment due on {nextPayment.Value:MMM dd}");
        }

        // Transaction frequency
        if (transactions.TotalTransactions > 100)
            insights.Add("High transaction volume detected. Consider reviewing your spending.");

        return insights;
    }

    private List<string> GenerateHealthRecommendations(
        decimal savingsRate, decimal debtToIncomeRatio, decimal emergencyFund)
    {
        var recommendations = new List<string>();

        if (savingsRate < 20)
            recommendations.Add("Try to save at least 20% of your income each month.");

        if (debtToIncomeRatio > 40)
            recommendations.Add("Your debt payments are high. Consider paying off high-interest debt first.");

        if (emergencyFund < 3)
            recommendations.Add("Build your emergency fund to cover 3-6 months of expenses.");

        if (recommendations.Count == 0)
            recommendations.Add("You're doing great! Keep up the good financial habits.");

        return recommendations;
    }

    private string GetTransactionTitle(Transaction transaction)
    {
        return transaction.Type switch
        {
            TransactionType.Deposit => "Deposit Received",
            TransactionType.Withdrawal => "Withdrawal",
            TransactionType.Transfer => "Transfer",
            TransactionType.ExchangeCurrency => "Currency Exchange",
            TransactionType.LoanPayment => "Loan Payment",
            TransactionType.Fee => "Service Fee",
            _ => "Transaction"
        };
    }

    private string GetActivityIcon(string type)
    {
        return type switch
        {
            "Deposit" => "arrow-down",
            "Withdrawal" => "arrow-up",
            "Transfer" => "arrows-right-left",
            "ExchangeCurrency" => "currency",
            "LoanPayment" => "credit-card",
            "Fee" => "receipt",
            _ => "activity"
        };
    }

    private async Task<List<FrequentRecipientDto>> GetFrequentRecipientsAsync(Guid userId)
    {
        var transfers = await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .ThenInclude(a => a.User)
            .Where(t => t.Type == TransactionType.Transfer &&
                       t.Account.UserId == userId &&
                       t.CreatedAt >= DateTime.UtcNow.AddMonths(-3))
            .GroupBy(t => t.ToAccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Count = g.Count(),
                LastTransfer = g.Max(t => t.CreatedAt),
                ToAccount = g.First().ToAccount
            })
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToListAsync();

        return transfers.Select(t => new FrequentRecipientDto
        {
            AccountNumber = t.ToAccount?.AccountNumber,
            Name = t.ToAccount?.User != null ?
                $"{t.ToAccount.User.FirstName} {t.ToAccount.User.LastName}" : "Unknown",
            TransferCount = t.Count,
            LastTransfer = t.LastTransfer
        }).ToList();
    }

    private async Task<List<FavoriteCurrencyPairSimpleDto>> GetFavoriteCurrencyPairsAsync(Guid userId)
    {
        var pairs = await _context.FavoriteCurrencyPairs
            .Where(p => p.UserId == userId)
            .Select(p => new FavoriteCurrencyPairSimpleDto
            {
                FromCurrency = p.FromCurrency,
                ToCurrency = p.ToCurrency
            })
            .Take(5)
            .ToListAsync();

        foreach (var pair in pairs)
        {
            pair.CurrentRate = await _currencyService.GetExchangeRateAsync(
                pair.FromCurrency, pair.ToCurrency);
        }

        return pairs;
    }

    private async Task<List<string>> GetSuggestedActionsAsync(Guid userId)
    {
        var suggestions = new List<string>();

        // Check for pending invoices
        var pendingInvoices = await _context.Invoices
            .CountAsync(i => i.UserId == userId &&
                           (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue));

        if (pendingInvoices > 0)
            suggestions.Add($"You have {pendingInvoices} pending invoice(s) to pay");

        // Check for upcoming loan payments
        var nextLoanPayment = await _context.Loans
            .Where(l => l.UserId == userId && l.Status == LoanStatus.Active)
            .MinAsync(l => l.NextPaymentDate);

        if (nextLoanPayment.HasValue && nextLoanPayment.Value < DateTime.UtcNow.AddDays(7))
            suggestions.Add("Loan payment due soon");

        // Check account balance
        var lowBalanceAccounts = await _context.Accounts
            .CountAsync(a => a.UserId == userId && a.IsActive && a.Balance < 100);

        if (lowBalanceAccounts > 0)
            suggestions.Add($"{lowBalanceAccounts} account(s) have low balance");

        return suggestions;
    }
}