using Application.Services;
using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentTypes()
        {
            var types = await _documentService.GetDocumentTypesAsync();
            return Ok(types);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDocumentDto>>> GetUserDocuments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var documents = await _documentService.GetUserDocumentsAsync(userId);
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDocumentDto>> GetDocument(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var document = await _documentService.GetUserDocumentByIdAsync(id, userId);
                return Ok(document);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(10_485_760)] // 10MB limit
        public async Task<ActionResult<UserDocumentDto>> UploadDocument([FromForm] DocumentUploadDto uploadDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (uploadDto.File == null || uploadDto.File.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            try
            {
                var document = await _documentService.UploadDocumentAsync(userId, new DocumentUploadRequestDto
                {
                    File = uploadDto.File,
                    DocumentTypeId = uploadDto.DocumentTypeId
                });
                return CreatedAtAction(nameof(GetDocument), new { id = document.DocumentId }, document);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, "An error occurred while uploading the document");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _documentService.DeleteDocumentAsync(id, userId);
                if (result)
                    return NoContent();
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("verification-status")]
        public async Task<ActionResult<UserVerificationStatusDto>> GetVerificationStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var status = await _documentService.GetUserVerificationStatusAsync(userId);
            return Ok(status);
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var document = await _documentService.GetUserDocumentByIdAsync(id, userId);
                var stream = await _documentService.DownloadDocumentAsync(id, userId);

                return File(stream, document.ContentType, document.OriginalFileName);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/verify")]
        public async Task<ActionResult> VerifyDocument(int id, [FromBody] DocumentVerificationDto verificationDto)
        {
            var verifiedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

            try
            {
                var result = await _documentService.VerifyDocumentAsync(
                    id,
                    verificationDto.Status,
                    verificationDto.Notes,
                    verifiedBy);

                if (result.Success)
                    return Ok();
                return BadRequest("Failed to verify document");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<DocumentVerificationHistoryDto>>> GetDocumentHistory(int id)
        {
            var history = await _documentService.GetDocumentHistoryAsync(id);
            return Ok(history);
        }
    }
}
