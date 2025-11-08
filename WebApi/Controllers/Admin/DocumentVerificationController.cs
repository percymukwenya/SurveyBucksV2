using Application.Services;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/document-verification")]
    [Authorize(Policy = "AdminOnly")]
    public class DocumentVerificationController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentVerificationController> _logger;

        public DocumentVerificationController(
            IDocumentService documentService,
            ILogger<DocumentVerificationController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard statistics for document verification
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<AdminDocumentStatsDto>> GetDocumentStats()
        {
            try
            {
                var stats = await _documentService.GetDocumentVerificationStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document verification statistics: {Error}", ex.Message);
                
                // Return mock data for now
                var mockStats = new AdminDocumentStatsDto
                {
                    TotalDocuments = 0,
                    PendingDocuments = 0,
                    ApprovedDocuments = 0,
                    RejectedDocuments = 0,
                    DocumentsToday = 0,
                    DocumentsThisWeek = 0,
                    DocumentsThisMonth = 0
                };
                return Ok(mockStats);
            }
        }

        /// <summary>
        /// Get pending documents for verification
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<UserDocumentDto>>> GetPendingDocuments(
            [FromQuery] int? documentTypeId = null,
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size
                if (pageSize < 1) pageSize = 10;
                if (pageNumber < 1) pageNumber = 1;

                var documents = await _documentService.GetPendingDocumentsAsync(documentTypeId, pageSize, pageNumber);
                var totalCount = await _documentService.GetPendingDocumentsCountAsync(documentTypeId);

                return Ok(new
                {
                    documents = documents,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending documents");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get documents by status (Approved, Rejected, Pending)
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<UserDocumentDto>>> GetDocumentsByStatus(
            string status,
            [FromQuery] int? documentTypeId = null,
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                var validStatuses = new[] { "Pending", "Approved", "Rejected" };
                if (!Array.Exists(validStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { error = "Invalid status. Valid values are: Pending, Approved, Rejected" });
                }

                if (pageSize > 100) pageSize = 100;
                if (pageSize < 1) pageSize = 10;
                if (pageNumber < 1) pageNumber = 1;

                var documents = await _documentService.GetDocumentsByStatusAsync(status, documentTypeId, pageSize, pageNumber);

                return Ok(new
                {
                    documents = documents,
                    status = status,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents by status {Status}", status);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Search documents by user email, name, document type, or filename
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDocumentDto>>> SearchDocuments(
            [FromQuery] string searchTerm,
            [FromQuery] string status = null,
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { error = "Search term is required" });
                }

                if (searchTerm.Length < 2)
                {
                    return BadRequest(new { error = "Search term must be at least 2 characters" });
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var validStatuses = new[] { "Pending", "Approved", "Rejected" };
                    if (!Array.Exists(validStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new { error = "Invalid status filter" });
                    }
                }

                if (pageSize > 100) pageSize = 100;
                if (pageSize < 1) pageSize = 10;
                if (pageNumber < 1) pageNumber = 1;

                var documents = await _documentService.SearchDocumentsAsync(searchTerm, status, pageSize, pageNumber);

                return Ok(new
                {
                    documents = documents,
                    searchTerm = searchTerm,
                    status = status,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Verify (approve or reject) a document
        /// </summary>
        [HttpPost("verify/{documentId}")]
        public async Task<ActionResult<DocumentVerificationResultDto>> VerifyDocument(
            int documentId,
            [FromBody] DocumentVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validStatuses = new[] { "Approved", "Rejected" };
                if (!Array.Exists(validStatuses, s => s.Equals(request.Status, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { error = "Status must be either 'Approved' or 'Rejected'" });
                }

                if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) && 
                    string.IsNullOrWhiteSpace(request.Notes))
                {
                    return BadRequest(new { error = "Notes are required when rejecting a document" });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? currentUserId;

                var result = await _documentService.VerifyDocumentAsync(
                    documentId, 
                    request.Status, 
                    request.Notes ?? "", 
                    currentUserEmail);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Document {DocumentId} verified by {AdminUser} with status {Status}", 
                    documentId, currentUserEmail, request.Status);

                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound(new { error = "Document not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document {DocumentId}", documentId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Download a document for verification
        /// </summary>
        [HttpGet("download/{documentId}")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var (fileStream, fileName, contentType) = await _documentService.DownloadDocumentForAdminAsync(documentId);

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Document {DocumentId} downloaded by admin {AdminUserId}", documentId, currentUserId);

                return File(fileStream, contentType, fileName);
            }
            catch (NotFoundException)
            {
                return NotFound(new { error = "Document not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get document verification history
        /// </summary>
        [HttpGet("history/{documentId}")]
        public async Task<ActionResult<IEnumerable<DocumentVerificationHistoryDto>>> GetDocumentHistory(int documentId)
        {
            try
            {
                var history = await _documentService.GetDocumentHistoryAsync(documentId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document history for {DocumentId}", documentId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all document types for filtering
        /// </summary>
        [HttpGet("document-types")]
        public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentTypes()
        {
            try
            {
                var documentTypes = await _documentService.GetDocumentTypesAsync();
                return Ok(documentTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document types");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Batch verify multiple documents
        /// </summary>
        [HttpPost("verify/batch")]
        public async Task<ActionResult<BatchVerificationResult>> BatchVerifyDocuments(
            [FromBody] BatchVerificationRequest request)
        {
            try
            {
                if (request.DocumentVerifications == null || !request.DocumentVerifications.Any())
                {
                    return BadRequest(new { error = "No documents specified for verification" });
                }

                if (request.DocumentVerifications.Count() > 50)
                {
                    return BadRequest(new { error = "Cannot verify more than 50 documents at once" });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? currentUserId;

                var results = new List<DocumentVerificationResultDto>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var verification in request.DocumentVerifications)
                {
                    try
                    {
                        var result = await _documentService.VerifyDocumentAsync(
                            verification.DocumentId, 
                            verification.Status, 
                            verification.Notes ?? "", 
                            currentUserEmail);

                        results.Add(result);

                        if (result.Success)
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to verify document {DocumentId} in batch", verification.DocumentId);
                        results.Add(new DocumentVerificationResultDto
                        {
                            DocumentId = verification.DocumentId,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        failureCount++;
                    }
                }

                _logger.LogInformation("Batch verification completed by {AdminUser}: {SuccessCount} successful, {FailureCount} failed", 
                    currentUserEmail, successCount, failureCount);

                return Ok(new BatchVerificationResult
                {
                    TotalProcessed = results.Count,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch document verification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    #region Request/Response Models

    public class DocumentVerificationRequest
    {
        public string Status { get; set; } = string.Empty; // "Approved" or "Rejected"
        public string Notes { get; set; } = string.Empty;
    }

    public class BatchVerificationRequest
    {
        public IEnumerable<DocumentVerificationItem> DocumentVerifications { get; set; } = Array.Empty<DocumentVerificationItem>();
    }

    public class DocumentVerificationItem
    {
        public int DocumentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class BatchVerificationResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public IEnumerable<DocumentVerificationResultDto> Results { get; set; } = Array.Empty<DocumentVerificationResultDto>();
    }

    #endregion
}