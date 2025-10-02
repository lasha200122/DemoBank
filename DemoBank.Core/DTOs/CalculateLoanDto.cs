using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class CalculateLoanDto
{
    [Required]
    [Range(1000, 100000)]
    public decimal Amount { get; set; }

    [Required]
    [Range(0, 50)]
    public decimal InterestRate { get; set; }

    [Required]
    [Range(6, 60)]
    public int TermMonths { get; set; }
}