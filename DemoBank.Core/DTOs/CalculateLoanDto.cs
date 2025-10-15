using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class CalculateLoanDto
{
    [Required]
    [Range(50_000, 1_000_000)]
    public decimal Amount { get; set; }

    [Required]
    [Range(0, 50)]
    public decimal InterestRate { get; set; }

    [Required]
    [Range(6, 72)]
    public int TermMonths { get; set; }
}