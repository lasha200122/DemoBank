using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class Loan
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public decimal InterestRate { get; set; }

    [Required]
    public int TermMonths { get; set; }

    public decimal MonthlyPayment { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }

    public LoanStatus Status { get; set; }
    public DateTime ApprovedDate { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; }
    public virtual ICollection<LoanPayment> Payments { get; set; }
}

public enum LoanStatus
{
    Pending,
    Approved,
    Rejected,
    Active,
    PaidOff,
    Defaulted
}