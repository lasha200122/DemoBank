using DemoBank.Core.Models;

namespace DemoBank.Core.DTOs;

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public string Type { get; set; }
    public string Currency { get; set; }
    public decimal Balance { get; set; }
    public bool IsPriority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    PotentialInvestmentRange PotentialInvestmentRange { get; set; }
}