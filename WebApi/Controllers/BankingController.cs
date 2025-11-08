using Application.Services;
using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankingController : ControllerBase
    {
        private readonly IBankingService _bankingService;
        private readonly ILogger<BankingController> _logger;

        public BankingController(IBankingService bankingService, ILogger<BankingController> logger)
        {
            _bankingService = bankingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BankingDetailDto>>> GetBankingDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var bankingDetails = await _bankingService.GetUserBankingDetailsAsync(userId);
            return Ok(bankingDetails);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BankingDetailDto>> GetBankingDetail(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var bankingDetail = await _bankingService.GetBankingDetailByIdAsync(id, userId);
                return Ok(bankingDetail);
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

        [HttpPost]
        public async Task<ActionResult<BankingDetailDto>> CreateBankingDetail([FromBody] CreateBankingDetailDto createDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var bankingDetail = await _bankingService.CreateBankingDetailAsync(userId, createDto);
                return CreatedAtAction(nameof(GetBankingDetail), new { id = bankingDetail.Id }, bankingDetail);
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
                _logger.LogError(ex, "Error creating banking detail");
                return StatusCode(500, "An error occurred while creating banking details");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateBankingDetail(int id, [FromBody] UpdateBankingDetailDto updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (id != updateDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                var result = await _bankingService.UpdateBankingDetailAsync(userId, updateDto);
                if (result)
                    return NoContent();
                return NotFound();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBankingDetail(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _bankingService.DeleteBankingDetailAsync(id, userId);
                if (result)
                    return NoContent();
                return NotFound();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{id}/set-primary")]
        public async Task<ActionResult> SetPrimaryBankingDetail(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _bankingService.SetPrimaryBankingDetailAsync(id, userId);
                if (result)
                    return Ok();
                return BadRequest("Failed to set primary banking detail");
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

        // Admin endpoints
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/verify")]
        public async Task<ActionResult> VerifyBankingDetail(int id)
        {
            var verifiedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

            try
            {
                var result = await _bankingService.VerifyBankingDetailAsync(id, verifiedBy);
                if (result)
                    return Ok();
                return BadRequest("Failed to verify banking detail");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying banking detail");
                return StatusCode(500, "An error occurred while verifying banking details");
            }
        }
    }
}
