using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class Investment
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid? PlanId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public decimal CustomROI { get; set; } // Custom ROI for this specific investment (annual %)

    public decimal BaseROI { get; set; } // Base ROI at time of investment

    public InvestmentStatus Status { get; set; }

    public InvestmentTerm Term { get; set; }

    public int TermMonths { get; set; } // Investment duration

    public decimal ProjectedReturn { get; set; } // Total projected return

    public decimal TotalPaidOut { get; set; } // Total amount paid out so far

    public DateTime StartDate { get; set; }

    public DateTime MaturityDate { get; set; }

    public DateTime? LastPayoutDate { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? RejectedDate { get; set; }

    public string? RejectionReason { get; set; }

    public bool AutoRenew { get; set; }
    public Guid? CustomRateId { get; set; }

    public decimal MinimumBalance { get; set; } // Minimum balance to maintain

    public PayoutFrequency PayoutFrequency { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; }
    public virtual InvestmentPlan Plan { get; set; }
    public virtual ICollection<InvestmentReturn> Returns { get; set; }
    public virtual InvestmentRate CustomRate { get; set; }
    public virtual ICollection<InvestmentTransaction> Transactions { get; set; }
}


public enum InvestmentStatus
{
    Pending,
    UnderReview,
    Approved,
    Active,
    Matured,
    Withdrawn,
    Rejected,
    Cancelled,
    OnHold
}

public enum InvestmentTerm
{
    ShortTerm,     // 1-6 months
    MediumTerm,    // 6-12 months
    LongTerm,      // 12+ months
    Custom
}


public enum PayoutFrequency
{
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually,
    AtMaturity
}