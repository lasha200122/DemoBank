using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class UserSettings
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(3)]
    public string PreferredCurrency { get; set; } = "USD";

    [MaxLength(10)]
    public string Language { get; set; } = "en";

    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool TwoFactorEnabled { get; set; } = false;

    public decimal DailyTransferLimit { get; set; } = 10000;
    public decimal DailyWithdrawalLimit { get; set; } = 5000;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual User User { get; set; }
}