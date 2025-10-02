using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class Notification
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [Required]
    public string Message { get; set; }

    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation property
    public virtual User User { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Transaction,
    Security
}