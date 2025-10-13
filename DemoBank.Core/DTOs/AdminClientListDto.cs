using DemoBank.Core.Models;

namespace DemoBank.Core.DTOs;

public class AdminClientListDto
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public PotentialInvestmentRange InvestmentRange { get; set; }
    public Status Status { get; set; }
    public bool EmailStatus { get; set; }
    public string Passkey { get; set; }
    public int ActiveAccounts { get; set; }
    public int ActiveInvestments { get; set; }
    public int ActiveLoans { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public decimal TotalBalanceUSD { get; set; }
    public CreateBankingDetailsDto BankingDetails { get; set; }
}
