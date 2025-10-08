using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.Models;

public class InvestmentRate
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; } // Null for global rates

    public Guid? PlanId { get; set; } // Null for user-specific rates across all plans

    [Required]
    [MaxLength(50)]
    public string RateType { get; set; } // "BASE", "BONUS", "PENALTY", etc.

    public decimal Rate { get; set; } // Percentage

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }

    [MaxLength(50)]
    public string CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; }
    public virtual InvestmentPlan Plan { get; set; }
}
