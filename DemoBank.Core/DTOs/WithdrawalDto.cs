using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBank.Core.DTOs;

public class WithdrawalDto
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } // Optional, defaults to account currency

    [MaxLength(500)]
    public string Description { get; set; }
}
