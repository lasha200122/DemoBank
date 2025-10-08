using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class InvestmentTransaction
{
    public Guid Id { get; set; }

    [Required]
    public Guid InvestmentId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    public InvestmentTransactionType Type { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public Guid? RelatedTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Investment Investment { get; set; }
    public virtual Account Account { get; set; }
}

public enum InvestmentTransactionType
{
    InitialDeposit,
    AdditionalDeposit,
    InterestPayout,
    DividendPayout,
    PartialWithdrawal,
    FullWithdrawal,
    PenaltyCharge,
    ManagementFee,
    PerformanceBonus
}