using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace DemoBank.Core.Models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    public PotentialInvestmentRange? PotentialInvestmentRange { get; set; }

    public string? Passkey { get; set; }
    

    public UserRole Role { get; set; }
    public Status Status { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Account> Accounts { get; set; }
    public virtual ICollection<Loan> Loans { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
    public virtual UserSettings Settings { get; set; }
    public virtual ICollection<Investment> Investments { get; set; }

    public virtual ICollection<BankingDetails> BankingDetails { get; set; }
}

public enum UserRole
{
    Client,
    Admin
}

public enum Status
{
    Pending = 0,
    Active = 1,
    Rejected = 2
}