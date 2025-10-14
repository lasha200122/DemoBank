using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoBank.Core.Models;
using Microsoft.AspNetCore.Http;

namespace DemoBank.Core.DTOs;

public class UploadDocumentDto
{
    [Required]
    public IFormFile File { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

public class AdminUploadDocumentDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public IFormFile File { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DocumentStatus Status { get; set; }
    public string StatusName { get; set; }
    public string RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public string DownloadUrl { get; set; }
}

public class ReviewDocumentDto
{
    [Required]
    public DocumentStatus Status { get; set; }

    [MaxLength(1000)]
    public string RejectionReason { get; set; }
}

public class KycStatusDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public KycVerificationStatus OverallStatus { get; set; }
    public List<DocumentVerificationDto> Documents { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public bool IsFullyVerified { get; set; }
}

public class DocumentVerificationDto
{
    public DocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSubmitted { get; set; }
    public DocumentStatus? Status { get; set; }
    public string StatusName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string RejectionReason { get; set; }
}

public class UserKycSummaryDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public KycVerificationStatus KycStatus { get; set; }
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public DateTime? LastDocumentUploadDate { get; set; }
    public DateTime? LastReviewDate { get; set; }
}

public class BulkReviewDocumentsDto
{
    [Required]
    public List<Guid> DocumentIds { get; set; }

    [Required]
    public DocumentStatus Status { get; set; }

    [MaxLength(1000)]
    public string RejectionReason { get; set; }
}

public enum KycVerificationStatus
{
    NotStarted = 0,
    Incomplete = 1,
    PendingReview = 2,
    UnderReview = 3,
    Verified = 4,
    Rejected = 5,
    RequiresResubmission = 6
}

public class DownloadDocumentDto
{
    public byte[] FileContent { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
}