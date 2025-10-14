using System.Security.Claims;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    // POST: api/Document/upload
    /// <summary>
    /// Upload a document for the current user
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto uploadDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto<object>.ErrorResponse("Invalid request data"));

            var userId = GetCurrentUserId();

            var result = await _documentService.UploadDocumentAsync(
                userId,
                uploadDto.File,
                uploadDto.DocumentType,
                uploadDto.ExpiryDate
            );

            return Ok(ResponseDto<DocumentDto>.SuccessResponse(result, "Document uploaded successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while uploading the document"));
        }
    }

    // POST: api/Document/admin/upload
    /// <summary>
    /// Upload a document for a specific user (Admin only)
    /// </summary>
    [HttpPost("admin/upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminUploadDocument([FromForm] AdminUploadDocumentDto uploadDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto<object>.ErrorResponse("Invalid request data"));

            var result = await _documentService.UploadDocumentAsync(
                uploadDto.UserId,
                uploadDto.File,
                uploadDto.DocumentType,
                uploadDto.ExpiryDate
            );

            return Ok(ResponseDto<DocumentDto>.SuccessResponse(result, "Document uploaded successfully for user"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for user");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while uploading the document"));
        }
    }

    // GET: api/Document/my-documents
    /// <summary>
    /// Get all documents for the current user
    /// </summary>
    [HttpGet("my-documents")]
    public async Task<IActionResult> GetMyDocuments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var documents = await _documentService.GetUserDocumentsAsync(userId);

            return Ok(ResponseDto<List<DocumentDto>>.SuccessResponse(documents));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user documents");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving documents"));
        }
    }

    // GET: api/Document/user/{userId}
    /// <summary>
    /// Get all documents for a specific user (Admin only)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserDocuments(Guid userId)
    {
        try
        {
            var documents = await _documentService.GetUserDocumentsAsync(userId);
            return Ok(ResponseDto<List<DocumentDto>>.SuccessResponse(documents));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user documents");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving documents"));
        }
    }

    // GET: api/Document/{id}/download
    /// <summary>
    /// Download a document by ID
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var result = await _documentService.DownloadDocumentAsync(id, userId, isAdmin);

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while downloading the document"));
        }
    }

    // GET: api/Document/kyc-status
    /// <summary>
    /// Get KYC verification status for the current user
    /// </summary>
    [HttpGet("kyc-status")]
    public async Task<IActionResult> GetMyKycStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _documentService.GetKycStatusAsync(userId);

            return Ok(ResponseDto<KycStatusDto>.SuccessResponse(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KYC status");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving KYC status"));
        }
    }

    // GET: api/Document/kyc-status/{userId}
    /// <summary>
    /// Get KYC verification status for a specific user (Admin only)
    /// </summary>
    [HttpGet("kyc-status/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserKycStatus(Guid userId)
    {
        try
        {
            var status = await _documentService.GetKycStatusAsync(userId);
            return Ok(ResponseDto<KycStatusDto>.SuccessResponse(status));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user KYC status");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving KYC status"));
        }
    }

    // PUT: api/Document/{id}/review
    /// <summary>
    /// Review a document (approve/reject) - Admin only
    /// </summary>
    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewDocument(Guid id, [FromBody] ReviewDocumentDto reviewDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto<object>.ErrorResponse("Invalid review data"));

            if (reviewDto.Status == DocumentStatus.Rejected && string.IsNullOrWhiteSpace(reviewDto.RejectionReason))
                return BadRequest(ResponseDto<object>.ErrorResponse("Rejection reason is required when rejecting a document"));

            var reviewerId = GetCurrentUserId();
            var result = await _documentService.ReviewDocumentAsync(
                id,
                reviewDto.Status,
                reviewDto.RejectionReason,
                reviewerId
            );

            return Ok(ResponseDto<DocumentDto>.SuccessResponse(result, "Document reviewed successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing document");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while reviewing the document"));
        }
    }

    // PUT: api/Document/bulk-review
    /// <summary>
    /// Bulk review documents - Admin only
    /// </summary>
    [HttpPut("bulk-review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkReviewDocuments([FromBody] BulkReviewDocumentsDto bulkReviewDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto<object>.ErrorResponse("Invalid review data"));

            if (bulkReviewDto.Status == DocumentStatus.Rejected && string.IsNullOrWhiteSpace(bulkReviewDto.RejectionReason))
                return BadRequest(ResponseDto<object>.ErrorResponse("Rejection reason is required when rejecting documents"));

            var reviewerId = GetCurrentUserId();
            var results = await _documentService.BulkReviewDocumentsAsync(
                bulkReviewDto.DocumentIds,
                bulkReviewDto.Status,
                bulkReviewDto.RejectionReason,
                reviewerId
            );

            return Ok(ResponseDto<List<DocumentDto>>.SuccessResponse(results, $"Successfully reviewed {results.Count} documents"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk reviewing documents");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while reviewing documents"));
        }
    }

    // DELETE: api/Document/{id}
    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            await _documentService.DeleteDocumentAsync(id, userId, isAdmin);

            return Ok(ResponseDto<object>.SuccessResponse(null, "Document deleted successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while deleting the document"));
        }
    }

    // GET: api/Document/admin/pending-reviews
    /// <summary>
    /// Get all users with pending KYC documents - Admin only
    /// </summary>
    [HttpGet("admin/pending-reviews")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingKycReviews()
    {
        try
        {
            var summaries = await _documentService.GetPendingKycReviewsAsync();
            return Ok(ResponseDto<List<UserKycSummaryDto>>.SuccessResponse(summaries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending KYC reviews");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving pending reviews"));
        }
    }

    // GET: api/Document/admin/all-kyc-status
    /// <summary>
    /// Get KYC status for all users - Admin only
    /// </summary>
    [HttpGet("admin/all-kyc-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUserKycStatus([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var summaries = await _documentService.GetAllUserKycStatusAsync(page, pageSize);
            return Ok(ResponseDto<List<UserKycSummaryDto>>.SuccessResponse(summaries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all user KYC status");
            return StatusCode(500, ResponseDto<object>.ErrorResponse("An error occurred while retrieving KYC status"));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }
}