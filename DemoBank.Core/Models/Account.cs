using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace DemoBank.Core.Models;

public class Account
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string AccountNumber { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public AccountType Type { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public decimal Balance { get; set; }
    public bool IsPriority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; }
}

public enum AccountType
{
    Checking,
    Savings,
    Investment
}