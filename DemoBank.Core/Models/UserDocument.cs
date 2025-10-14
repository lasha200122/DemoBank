using System.ComponentModel.DataAnnotations;

namespace DemoBank.Core.Models;

public class UserDocument
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FileId { get; set; } // MinIO object name

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    [MaxLength(100)]
    public string ContentType { get; set; }

    public long FileSize { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    [MaxLength(1000)]
    public string RejectionReason { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ExpiryDate { get; set; } // For documents like passport

    // Navigation properties
    public virtual User User { get; set; }
    public virtual User Reviewer { get; set; }
}


public enum DocumentType
{
    Passport = 0,
    DriversLicense = 1,
    NationalId = 2,
    BankStatement = 3,
    ProofOfAddress = 4,
    Other = 5
}

public enum DocumentStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4
}