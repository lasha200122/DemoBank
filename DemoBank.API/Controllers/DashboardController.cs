using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICurrencyService _currencyService;
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public DashboardController(
        IAccountService accountService,
        ITransactionService transactionService,
        ICurrencyService currencyService,
        IUserService userService,
        IMapper mapper)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _currencyService = currencyService;
        _userService = userService;
        _mapper = mapper;
    }

    // GET: api/Dashboard
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetByIdAsync(userId);

            // Get accounts summary
            var accounts = await _accountService.GetActiveUserAccountsAsync(userId);
            var totalBalanceUSD = await _accountService.GetTotalBalanceInUSDAsync(userId);
            var balancesByCurrency = await _accountService.GetBalancesByCurrencyAsync(userId);

            // Get recent transactions (last 10)
            var recentTransactions = await _transactionService.GetUserTransactionsAsync(userId, 10);

            // Get today's statistics
            var todayStats = await GetTodayStatistics(userId);

            // Get monthly statistics
            var currentMonth = DateTime.UtcNow;
            var monthlyStats = await _transactionService.GetMonthlyStatisticsAsync(
                userId,
                currentMonth.Year,
                currentMonth.Month
            );

            // Get transaction limits
            var withdrawalLimit = user.Settings?.DailyWithdrawalLimit ?? 5000;
            var transferLimit = user.Settings?.DailyTransferLimit ?? 10000;
            var todayWithdrawals = await _transactionService.GetDailyWithdrawalTotalAsync(
                userId,
                DateTime.UtcNow
            );
            var todayTransfers = await _transactionService.GetDailyTransferTotalAsync(
                userId,
                DateTime.UtcNow
            );

            var dashboard = new DashboardDto
            {
                User = new DashboardUserDto
                {
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    PreferredCurrency = user.Settings?.PreferredCurrency ?? "USD"
                },
                AccountSummary = new DashboardAccountSummaryDto
                {
                    TotalAccounts = accounts.Count,
                    TotalBalanceUSD = totalBalanceUSD,
                    BalancesByCurrency = balancesByCurrency,
                    Accounts = _mapper.Map<List<AccountDto>>(accounts.Take(3)) // Show top 3 accounts
                },
                RecentActivity = new DashboardRecentActivityDto
                {
                    RecentTransactions = _mapper.Map<List<TransactionDto>>(recentTransactions),
                    TodayDeposits = todayStats.Deposits,
                    TodayWithdrawals = todayStats.Withdrawals,
                    TodayTransfers = todayStats.Transfers
                },
                MonthlyStatistics = new DashboardMonthlyStatsDto
                {
                    Month = currentMonth.ToString("MMMM yyyy"),
                    TotalDeposits = monthlyStats.GetValueOrDefault("TotalDeposits", 0),
                    TotalWithdrawals = monthlyStats.GetValueOrDefault("TotalWithdrawals", 0),
                    TotalTransfers = monthlyStats.GetValueOrDefault("TotalTransfers", 0),
                    NetCashFlow = monthlyStats.GetValueOrDefault("NetCashFlow", 0)
                },
                Limits = new DashboardLimitsDto
                {
                    DailyWithdrawalLimit = withdrawalLimit,
                    DailyTransferLimit = transferLimit,
                    UsedWithdrawalToday = todayWithdrawals,
                    UsedTransferToday = todayTransfers,
                    WithdrawalPercentageUsed = (todayWithdrawals / withdrawalLimit) * 100,
                    TransferPercentageUsed = (todayTransfers / transferLimit) * 100
                }
            };

            return Ok(ResponseDto<DashboardDto>.SuccessResponse(dashboard));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while loading dashboard"
            ));
        }
    }

    // GET: api/Dashboard/quick-stats
    [HttpGet("quick-stats")]
    public async Task<IActionResult> GetQuickStats()
    {
        try
        {
            var userId = GetCurrentUserId();

            var totalBalance = await _accountService.GetTotalBalanceInUSDAsync(userId);
            var accounts = await _accountService.GetActiveUserAccountsAsync(userId);
            var todayStats = await GetTodayStatistics(userId);

            var quickStats = new QuickStatsDto
            {
                TotalBalanceUSD = totalBalance,
                ActiveAccounts = accounts.Count,
                TodayTransactions = todayStats.TotalCount,
                LastTransactionTime = todayStats.LastTransactionTime
            };

            return Ok(ResponseDto<QuickStatsDto>.SuccessResponse(quickStats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching quick stats"
            ));
        }
    }

    // GET: api/Dashboard/recent-activity
    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 30)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Days must be between 1 and 30"
                ));
            }

            var userId = GetCurrentUserId();
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;

            var summary = await _transactionService.GetTransactionSummaryAsync(
                userId,
                startDate,
                endDate
            );

            var transactions = await _transactionService.GetUserTransactionsAsync(userId, 50);
            var recentTransactions = transactions
                .Where(t => t.CreatedAt >= startDate)
                .ToList();

            var activity = new RecentActivityDto
            {
                Period = $"Last {days} days",
                StartDate = startDate,
                EndDate = endDate,
                TotalTransactions = summary.TotalTransactions,
                TotalDeposits = summary.TotalDepositsUSD,
                TotalWithdrawals = summary.TotalWithdrawalsUSD,
                TotalTransfers = summary.TotalTransfersUSD,
                Transactions = _mapper.Map<List<TransactionDto>>(recentTransactions)
            };

            return Ok(ResponseDto<RecentActivityDto>.SuccessResponse(activity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching recent activity"
            ));
        }
    }

    private async Task<TodayStatistics> GetTodayStatistics(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayTransactions = await _transactionService.GetUserTransactionsAsync(userId, 100);
        var todayOnly = todayTransactions.Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow).ToList();

        decimal deposits = 0, withdrawals = 0, transfers = 0;

        foreach (var trans in todayOnly)
        {
            var amountUSD = trans.Currency == "USD"
                ? trans.Amount
                : await _currencyService.ConvertCurrencyAsync(trans.Amount, trans.Currency, "USD");

            switch (trans.Type)
            {
                case Core.Models.TransactionType.Deposit:
                    deposits += amountUSD;
                    break;
                case Core.Models.TransactionType.Withdrawal:
                    withdrawals += amountUSD;
                    break;
                case Core.Models.TransactionType.Transfer:
                    transfers += amountUSD;
                    break;
            }
        }

        return new TodayStatistics
        {
            Deposits = deposits,
            Withdrawals = withdrawals,
            Transfers = transfers,
            TotalCount = todayOnly.Count,
            LastTransactionTime = todayOnly.MaxBy(t => t.CreatedAt)?.CreatedAt
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }

    private class TodayStatistics
    {
        public decimal Deposits { get; set; }
        public decimal Withdrawals { get; set; }
        public decimal Transfers { get; set; }
        public int TotalCount { get; set; }
        public DateTime? LastTransactionTime { get; set; }
    }
}