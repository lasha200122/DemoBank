using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class AccountSummaryDto
{
    public decimal TotalBalanceUSD { get; set; }
    public Dictionary<string, decimal> BalancesByCurrency { get; set; }
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public List<AccountDto> Accounts { get; set; }
    public decimal MonthlyReturnsUSD { get; set; }
    public decimal YearlyReturnsUSD { get; set; }
    public decimal MonthlyReturnsEUR { get; set; }
    public decimal YearlyReturnsEUR { get; set; }
}