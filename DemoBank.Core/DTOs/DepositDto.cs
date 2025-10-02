using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class DepositDto
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