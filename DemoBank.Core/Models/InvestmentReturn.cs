using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class InvestmentReturn
{
    public Guid Id { get; set; }

    [Required]
    public Guid InvestmentId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; }

    public decimal InterestAmount { get; set; }

    public decimal PrincipalAmount { get; set; }

    public ReturnType Type { get; set; }

    public DateTime PaymentDate { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public PaymentStatus Status { get; set; }

    public Guid? TransactionId { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public virtual Investment Investment { get; set; }
}

public enum ReturnType
{
    Interest,
    Dividend,
    Capital,
    Bonus,
    Penalty
}

public enum PaymentStatus
{
    Scheduled,
    Processing,
    Completed,
    Failed,
    Cancelled
}