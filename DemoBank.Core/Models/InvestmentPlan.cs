using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class InvestmentPlan
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public InvestmentPlanType Type { get; set; }

    public decimal MinimumInvestment { get; set; }

    public decimal MaximumInvestment { get; set; }

    public decimal BaseROI { get; set; } // Annual percentage

    public int MinTermMonths { get; set; }

    public int MaxTermMonths { get; set; }

    public PayoutFrequency DefaultPayoutFrequency { get; set; }

    public bool IsActive { get; set; }

    public bool RequiresApproval { get; set; }

    public decimal EarlyWithdrawalPenalty { get; set; } // Percentage

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    // Risk metrics
    public RiskLevel RiskLevel { get; set; }

    public decimal VolatilityIndex { get; set; } // 0-100

    // Tier-based rates
    public string TierRatesJson { get; set; } // JSON serialized tier rates

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(50)]
    public string CreatedBy { get; set; }

    [MaxLength(50)]
    public string UpdatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<Investment> Investments { get; set; }
}

public enum InvestmentPlanType
{
    FixedDeposit,
    MutualFund,
    Stocks,
    Bonds,
    RealEstate,
    Crypto,
    Mixed,
    Custom
}

public enum RiskLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}