using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class CreateAccountDto
{
    [Required]
    public string Type { get; set; } // Checking, Savings, Investment

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public decimal InitialDeposit { get; set; }
    public bool IsPriority { get; set; }
}