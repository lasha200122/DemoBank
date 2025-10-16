using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class TransactionSummaryDto
{
    public decimal TotalDepositsEUR { get; set; }
    public decimal TotalWithdrawalsEUR { get; set; }
    public decimal TotalTransfersEUR { get; set; }
    public int DepositCount { get; set; }
    public int WithdrawalCount { get; set; }
    public int TransferCount { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}