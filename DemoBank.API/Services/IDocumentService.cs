using DemoBank.Core.DTOs;
using DemoBank.Core.Models;

namespace DemoBank.API.Services;

public interface IDocumentService
{
    Task<DocumentDto> UploadDocumentAsync(Guid userId, IFormFile file, DocumentType documentType, DateTime? expiryDate);
    Task<List<DocumentDto>> GetUserDocumentsAsync(Guid userId);
    Task<UserDocument> GetDocumentByIdAsync(Guid documentId);
    Task<DownloadDocumentDto> DownloadDocumentAsync(Guid documentId, Guid requestingUserId, bool isAdmin);
    Task<DocumentDto> ReviewDocumentAsync(Guid documentId, DocumentStatus status, string rejectionReason, Guid reviewerId);
    Task<List<DocumentDto>> BulkReviewDocumentsAsync(List<Guid> documentIds, DocumentStatus status, string rejectionReason, Guid reviewerId);
    Task DeleteDocumentAsync(Guid documentId, Guid userId, bool isAdmin);
    Task<KycStatusDto> GetKycStatusAsync(Guid userId);
    Task<List<UserKycSummaryDto>> GetPendingKycReviewsAsync();
    Task<List<UserKycSummaryDto>> GetAllUserKycStatusAsync(int page = 1, int pageSize = 50);
}