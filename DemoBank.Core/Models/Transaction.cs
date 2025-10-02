using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace DemoBank.Core.Models;

public class Transaction
{
    public Guid Id { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    public TransactionType Type { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; }

    public decimal? ExchangeRate { get; set; }
    public decimal AmountInAccountCurrency { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public Guid? RelatedTransactionId { get; set; } // For transfers
    public Guid? ToAccountId { get; set; } // For transfers

    public decimal BalanceAfter { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Account Account { get; set; }
    public virtual Account ToAccount { get; set; }
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    ExchangeCurrency,
    LoanPayment,
    Interest,
    Fee
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}