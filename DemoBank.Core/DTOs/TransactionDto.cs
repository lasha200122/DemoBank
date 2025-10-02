using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal AmountInAccountCurrency { get; set; }
    public string Description { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ToAccountNumber { get; set; } // For transfers
}