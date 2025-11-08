using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Constants;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace WebApi.Controllers.Admin
{
    [Authorize]
    [Route("api/admin/banking-verification")]
    [ApiController]
    public class BankingVerificationController : ControllerBase
    {
        private readonly IBankingService _bankingService;
        private readonly ILogger<BankingVerificationController> _logger;

        public BankingVerificationController(
            IBankingService bankingService,
            ILogger<BankingVerificationController> logger)
        {
            _bankingService = bankingService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<BankingVerificationStatsDto>> GetBankingVerificationStats()
        {
            try
            {
                var stats = await _bankingService.GetBankingVerificationStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banking verification stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<BankingDetailDto>>> GetPendingBankingDetails(
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                var pendingDetails = await _bankingService.GetBankingDetailsByStatusAsync("Pending", pageSize, pageNumber);
                return Ok(pendingDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending banking details");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("approved")]
        public async Task<ActionResult<IEnumerable<BankingDetailDto>>> GetApprovedBankingDetails(
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                var approvedDetails = await _bankingService.GetBankingDetailsByStatusAsync("Approved", pageSize, pageNumber);
                return Ok(approvedDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approved banking details");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("rejected")]
        public async Task<ActionResult<IEnumerable<BankingDetailDto>>> GetRejectedBankingDetails(
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                var rejectedDetails = await _bankingService.GetBankingDetailsByStatusAsync("Rejected", pageSize, pageNumber);
                return Ok(rejectedDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rejected banking details");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("verify/{bankingDetailId}")]
        public async Task<ActionResult<BankingVerificationResultDto>> VerifyBankingDetail(
            int bankingDetailId,
            [FromBody] BankingVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate status
                if (!IsValidVerificationStatus(request.Status))
                {
                    return BadRequest(new { error = "Invalid verification status. Must be 'Approved' or 'Rejected'" });
                }

                if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) && 
                    string.IsNullOrWhiteSpace(request.Notes))
                {
                    return BadRequest(new { error = "Notes are required when rejecting banking details" });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? currentUserId;

                var result = await _bankingService.VerifyBankingDetailAsync(
                    bankingDetailId, 
                    request.Status, 
                    request.Notes ?? "", 
                    currentUserEmail);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Banking detail {BankingDetailId} {Status} by {VerifiedBy}", 
                    bankingDetailId, request.Status, currentUserEmail);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying banking detail {BankingDetailId}", bankingDetailId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("verify/batch")]
        public async Task<ActionResult<BatchBankingVerificationResult>> BatchVerifyBankingDetails(
            [FromBody] BatchBankingVerificationRequest request)
        {
            try
            {
                if (request.BankingVerifications == null || !request.BankingVerifications.Any())
                {
                    return BadRequest(new { error = "No banking details specified for verification" });
                }

                if (request.BankingVerifications.Count() > 50)
                {
                    return BadRequest(new { error = "Maximum 50 banking details can be verified at once" });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? currentUserId;

                var results = new List<BankingVerificationResultDto>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var verification in request.BankingVerifications)
                {
                    try
                    {
                        var result = await _bankingService.VerifyBankingDetailAsync(
                            verification.BankingDetailId, 
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
                        _logger.LogError(ex, "Error verifying banking detail {BankingDetailId} in batch", verification.BankingDetailId);
                        
                        results.Add(new BankingVerificationResultDto
                        {
                            Success = false,
                            ErrorMessage = "Internal server error"
                        });
                        failureCount++;
                    }
                }

                _logger.LogInformation("Batch verification completed: {SuccessCount} successes, {FailureCount} failures", 
                    successCount, failureCount);

                return Ok(new BatchBankingVerificationResult
                {
                    TotalProcessed = results.Count,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch banking verification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BankingDetailDto>>> SearchBankingDetails(
            [FromQuery] string searchTerm,
            [FromQuery] string status = "",
            [FromQuery] int pageSize = 50,
            [FromQuery] int pageNumber = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { error = "Search term is required" });
                }

                var results = await _bankingService.SearchBankingDetailsAsync(searchTerm, status, pageSize, pageNumber);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching banking details with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private bool IsValidVerificationStatus(string status)
        {
            return status.Equals(VerificationStatus.Approved, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(VerificationStatus.Rejected, StringComparison.OrdinalIgnoreCase);
        }
    }

    #region Request/Response Models

    public class BankingVerificationRequest
    {
        public string Status { get; set; } = string.Empty; // "Approved" or "Rejected"
        public string Notes { get; set; } = string.Empty;
    }

    public class BatchBankingVerificationRequest
    {
        public IEnumerable<BankingVerificationItem> BankingVerifications { get; set; } = new List<BankingVerificationItem>();
    }

    public class BankingVerificationItem
    {
        public int BankingDetailId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class BatchBankingVerificationResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public IEnumerable<BankingVerificationResultDto> Results { get; set; } = new List<BankingVerificationResultDto>();
    }

    #endregion
}