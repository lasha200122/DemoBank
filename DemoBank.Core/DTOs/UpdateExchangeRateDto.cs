using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.DTOs;

public class UpdateExchangeRateDto
{
    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal NewRate { get; set; }
}