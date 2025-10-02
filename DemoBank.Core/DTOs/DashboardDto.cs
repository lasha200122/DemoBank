using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class DashboardDto
{
    public DashboardUserDto User { get; set; }
    public DashboardAccountSummaryDto AccountSummary { get; set; }
    public DashboardRecentActivityDto RecentActivity { get; set; }
    public DashboardMonthlyStatsDto MonthlyStatistics { get; set; }
    public DashboardLimitsDto Limits { get; set; }
}

public class DashboardUserDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string PreferredCurrency { get; set; }
}

public class DashboardAccountSummaryDto
{
    public int TotalAccounts { get; set; }
    public decimal TotalBalanceUSD { get; set; }
    public Dictionary<string, decimal> BalancesByCurrency { get; set; }
    public List<AccountDto> Accounts { get; set; }
}

public class DashboardRecentActivityDto
{
    public List<TransactionDto> RecentTransactions { get; set; }
    public decimal TodayDeposits { get; set; }
    public decimal TodayWithdrawals { get; set; }
    public decimal TodayTransfers { get; set; }
}

public class DashboardMonthlyStatsDto
{
    public string Month { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalTransfers { get; set; }
    public decimal NetCashFlow { get; set; }
}

public class DashboardLimitsDto
{
    public decimal DailyWithdrawalLimit { get; set; }
    public decimal DailyTransferLimit { get; set; }
    public decimal UsedWithdrawalToday { get; set; }
    public decimal UsedTransferToday { get; set; }
    public decimal WithdrawalPercentageUsed { get; set; }
    public decimal TransferPercentageUsed { get; set; }
}

public class QuickStatsDto
{
    public decimal TotalBalanceUSD { get; set; }
    public int ActiveAccounts { get; set; }
    public int TodayTransactions { get; set; }
    public DateTime? LastTransactionTime { get; set; }
}

public class RecentActivityDto
{
    public string Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalTransfers { get; set; }
    public List<TransactionDto> Transactions { get; set; }
}