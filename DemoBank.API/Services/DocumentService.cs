using AutoMapper;
using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DemoBank.API.Services;

public class DocumentService : IDocumentService
{
    private readonly DemoBankContext _context;
    private readonly IMinioService _minioService;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;
    private const string BUCKET_NAME = "kyc-documents";
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public DocumentService(
        DemoBankContext context,
        IMinioService minioService,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _minioService = minioService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadDocumentAsync(Guid userId, IFormFile file, DocumentType documentType, DateTime? expiryDate)
    {
        // Validate file
        ValidateFile(file);

        // Check if user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Upload to MinIO
        var objectName = $"{userId}/{documentType}/{Guid.NewGuid()}_{file.FileName}";
        var fileId = await _minioService.UploadFileAsync(file, BUCKET_NAME, objectName);

        // Save document record
        var document = new UserDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileId = fileId,
            FileName = file.FileName,
            DocumentType = documentType,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow,
            ExpiryDate = expiryDate,
            RejectionReason = string.Empty,
            ReviewedBy = userId
        };

        _context.UserDocuments.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document uploaded for user {UserId}: {DocumentType}", userId, documentType);

        return MapToDto(document);
    }

    public async Task<List<DocumentDto>> GetUserDocumentsAsync(Guid userId)
    {
        var documents = await _context.UserDocuments
            .Include(d => d.Reviewer)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<UserDocument> GetDocumentByIdAsync(Guid documentId)
    {
        var document = await _context.UserDocuments
            .Include(d => d.User)
            .Include(d => d.Reviewer)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            throw new InvalidOperationException("Document not found");

        return document;
    }

    public async Task<DownloadDocumentDto> DownloadDocumentAsync(Guid documentId, Guid requestingUserId, bool isAdmin)
    {
        var document = await GetDocumentByIdAsync(documentId);

        // Check permissions
        if (!isAdmin && document.UserId != requestingUserId)
            throw new UnauthorizedAccessException("You don't have permission to download this document");

        // Download from MinIO
        var (fileContent, contentType) = await _minioService.DownloadFileAsync(BUCKET_NAME, document.FileId);

        return new DownloadDocumentDto
        {
            FileContent = fileContent,
            FileName = document.FileName,
            ContentType = contentType
        };
    }

    public async Task<DocumentDto> ReviewDocumentAsync(Guid documentId, DocumentStatus status, string rejectionReason, Guid reviewerId)
    {
        if (status != DocumentStatus.Approved && status != DocumentStatus.Rejected)
            throw new ArgumentException("Invalid status. Must be Approved or Rejected");

        var document = await GetDocumentByIdAsync(documentId);

        if (document.Status != DocumentStatus.Pending && document.Status != DocumentStatus.UnderReview)
            throw new InvalidOperationException("Document has already been reviewed");

        document.Status = status;
        document.RejectionReason = status == DocumentStatus.Rejected ? rejectionReason : string.Empty;
        document.ReviewedBy = reviewerId;
        document.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} reviewed by {ReviewerId}: {Status}",
            documentId, reviewerId, status);

        return MapToDto(document);
    }

    public async Task<List<DocumentDto>> BulkReviewDocumentsAsync(List<Guid> documentIds, DocumentStatus status, string rejectionReason, Guid reviewerId)
    {
        var documents = await _context.UserDocuments
            .Where(d => documentIds.Contains(d.Id))
            .ToListAsync();

        foreach (var document in documents)
        {
            if (document.Status == DocumentStatus.Pending || document.Status == DocumentStatus.UnderReview)
            {
                document.Status = status;
                document.RejectionReason = status == DocumentStatus.Rejected ? rejectionReason : null;
                document.ReviewedBy = reviewerId;
                document.ReviewedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk review completed for {Count} documents by {ReviewerId}",
            documents.Count, reviewerId);

        return documents.Select(MapToDto).ToList();
    }

    public async Task DeleteDocumentAsync(Guid documentId, Guid userId, bool isAdmin)
    {
        var document = await GetDocumentByIdAsync(documentId);

        // Check permissions
        if (!isAdmin && document.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this document");

        // Delete from MinIO
        await _minioService.DeleteFileAsync(BUCKET_NAME, document.FileId);

        // Delete from database
        _context.UserDocuments.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} deleted", documentId);
    }

    public async Task<KycStatusDto> GetKycStatusAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var documents = await _context.UserDocuments
            .Where(d => d.UserId == userId)
            .ToListAsync();

        var requiredDocTypes = new[] { DocumentType.Passport, DocumentType.DriversLicense, DocumentType.NationalId };

        var documentVerifications = requiredDocTypes.Select(docType =>
        {
            var doc = documents.FirstOrDefault(d => d.DocumentType == docType && d.Status != DocumentStatus.Rejected);
            return new DocumentVerificationDto
            {
                DocumentType = docType,
                DocumentTypeName = docType.ToString(),
                IsRequired = true,
                IsSubmitted = doc != null,
                Status = doc?.Status,
                StatusName = doc?.Status.ToString(),
                SubmittedAt = doc?.UploadedAt,
                ReviewedAt = doc?.ReviewedAt,
                RejectionReason = doc?.RejectionReason
            };
        }).ToList();

        var pendingCount = documents.Count(d => d.Status == DocumentStatus.Pending);
        var approvedCount = documents.Count(d => d.Status == DocumentStatus.Approved);
        var rejectedCount = documents.Count(d => d.Status == DocumentStatus.Rejected);

        var overallStatus = DetermineKycStatus(documentVerifications, documents);
        var isFullyVerified = documentVerifications.All(d => d.Status == DocumentStatus.Approved);

        return new KycStatusDto
        {
            UserId = userId,
            UserName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            EmailVerified = user.Status == Status.Active,
            OverallStatus = overallStatus,
            Documents = documentVerifications,
            LastUpdateDate = documents.Any() ? documents.Max(d => d.ReviewedAt ?? d.UploadedAt) : null,
            PendingDocuments = pendingCount,
            ApprovedDocuments = approvedCount,
            RejectedDocuments = rejectedCount,
            IsFullyVerified = isFullyVerified
        };
    }

    public async Task<List<UserKycSummaryDto>> GetPendingKycReviewsAsync()
    {
        var usersWithPendingDocs = await _context.UserDocuments
            .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.UnderReview)
            .Select(d => d.UserId)
            .Distinct()
            .ToListAsync();

        return await GetKycSummariesForUsers(usersWithPendingDocs);
    }

    public async Task<List<UserKycSummaryDto>> GetAllUserKycStatusAsync(int page = 1, int pageSize = 50)
    {
        var userIds = await _context.Users
            .Where(u => u.Role == UserRole.Client)
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync();

        return await GetKycSummariesForUsers(userIds);
    }

    private async Task<List<UserKycSummaryDto>> GetKycSummariesForUsers(List<Guid> userIds)
    {
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var documents = await _context.UserDocuments
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync();

        var summaries = users.Select(user =>
        {
            var userDocs = documents.Where(d => d.UserId == user.Id).ToList();
            var status = DetermineUserKycStatus(userDocs);

            return new UserKycSummaryDto
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                KycStatus = status,
                TotalDocuments = userDocs.Count,
                PendingDocuments = userDocs.Count(d => d.Status == DocumentStatus.Pending),
                ApprovedDocuments = userDocs.Count(d => d.Status == DocumentStatus.Approved),
                RejectedDocuments = userDocs.Count(d => d.Status == DocumentStatus.Rejected),
                LastDocumentUploadDate = userDocs.Any() ? userDocs.Max(d => d.UploadedAt) : (DateTime?)null,
                LastReviewDate = userDocs.Any(d => d.ReviewedAt.HasValue)
                    ? userDocs.Where(d => d.ReviewedAt.HasValue).Max(d => d.ReviewedAt.Value)
                    : (DateTime?)null
            };
        }).ToList();

        return summaries;
    }

    private KycVerificationStatus DetermineKycStatus(List<DocumentVerificationDto> documentVerifications, List<UserDocument> allDocuments)
    {
        if (!allDocuments.Any())
            return KycVerificationStatus.NotStarted;

        var hasRejected = allDocuments.Any(d => d.Status == DocumentStatus.Rejected);
        var hasPending = allDocuments.Any(d => d.Status == DocumentStatus.Pending);
        var hasUnderReview = allDocuments.Any(d => d.Status == DocumentStatus.UnderReview);
        var allApproved = documentVerifications.All(d => d.IsSubmitted && d.Status == DocumentStatus.Approved);

        if (allApproved)
            return KycVerificationStatus.Verified;

        if (hasRejected)
            return KycVerificationStatus.RequiresResubmission;

        if (hasUnderReview)
            return KycVerificationStatus.UnderReview;

        if (hasPending)
            return KycVerificationStatus.PendingReview;

        return KycVerificationStatus.Incomplete;
    }

    private KycVerificationStatus DetermineUserKycStatus(List<UserDocument> documents)
    {
        if (!documents.Any())
            return KycVerificationStatus.NotStarted;

        var requiredDocTypes = new[] { DocumentType.Passport, DocumentType.DriversLicense, DocumentType.NationalId };
        var hasRequiredDoc = documents.Any(d => requiredDocTypes.Contains(d.DocumentType) && d.Status == DocumentStatus.Approved);

        if (hasRequiredDoc && documents.All(d => d.Status == DocumentStatus.Approved))
            return KycVerificationStatus.Verified;

        if (documents.Any(d => d.Status == DocumentStatus.Rejected))
            return KycVerificationStatus.RequiresResubmission;

        if (documents.Any(d => d.Status == DocumentStatus.UnderReview))
            return KycVerificationStatus.UnderReview;

        if (documents.Any(d => d.Status == DocumentStatus.Pending))
            return KycVerificationStatus.PendingReview;

        return KycVerificationStatus.Incomplete;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024}MB");

        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
            throw new ArgumentException($"File type {file.ContentType} is not allowed. Allowed types: PDF, JPEG, PNG, GIF, DOC, DOCX");
    }

    private DocumentDto MapToDto(UserDocument document)
    {
        var isExpired = document.ExpiryDate.HasValue && document.ExpiryDate.Value < DateTime.UtcNow;

        return new DocumentDto
        {
            Id = document.Id,
            UserId = document.UserId,
            FileId = document.FileId,
            FileName = document.FileName,
            DocumentType = document.DocumentType,
            DocumentTypeName = document.DocumentType.ToString(),
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = isExpired ? DocumentStatus.Expired : document.Status,
            StatusName = (isExpired ? DocumentStatus.Expired : document.Status).ToString(),
            RejectionReason = document.RejectionReason,
            ReviewedBy = document.ReviewedBy,
            ReviewedByName = document.Reviewer != null ? $"{document.Reviewer.FirstName} {document.Reviewer.LastName}" : null,
            ReviewedAt = document.ReviewedAt,
            UploadedAt = document.UploadedAt,
            ExpiryDate = document.ExpiryDate,
            IsExpired = isExpired,
            DownloadUrl = null // Can be set if needed
        };
    }
}