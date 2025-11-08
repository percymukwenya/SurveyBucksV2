using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Request;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IDemographicsRepository _demographicsRepository;
        private readonly IUserProfileCompletionService _userProfileCompletionService;

        public UserProfileController(IUserProfileService userProfileService, IDemographicsRepository demographicsRepository, IUserProfileCompletionService userProfileCompletionService)
        {
            _userProfileService = userProfileService;
            _demographicsRepository = demographicsRepository;
            _userProfileCompletionService = userProfileCompletionService;
        }

        [HttpGet("demographics")]
        public async Task<ActionResult<DemographicsDto>> GetUserDemographics()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var demographics = await _userProfileService.GetUserDemographicsAsync(userId);
            return Ok(demographics);
        }

        [HttpPut("demographics")]
        public async Task<ActionResult> UpdateDemographics(UpdateDemographicsRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Ensure the user can only update their own profile
            request.UserId = userId;

            try
            {
                var updateSuccess = await _userProfileService.UpdateDemographicsAsync(request, userId);
                if (!updateSuccess)
                {
                    return BadRequest(new ProfileUpdateResultDto
                    {
                        Success = false,
                        ErrorMessage = "Failed to update demographics",
                        SectionUpdated = "Demographics"
                    });
                }

                var result = await _userProfileCompletionService.ProcessProfileUpdateAsync(userId, "Demographics");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProfileUpdateResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    SectionUpdated = "Demographics"
                });
            }
        }

        [HttpGet("completion")]
        public async Task<ActionResult<decimal>> GetProfileCompletion()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var completion = await _userProfileService.GetProfileCompletionPercentageAsync(userId);
            return Ok(completion);
        }

        [HttpGet("interests")]
        public async Task<ActionResult<IEnumerable<UserInterestDto>>> GetUserInterests()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var interests = await _userProfileService.GetUserInterestsAsync(userId);
            return Ok(interests);
        }

        [HttpPost("interests")]
        public async Task<ActionResult> AddUserInterest(AddInterestRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _userProfileService.AddUserInterestAsync(userId, request.Interest, request.InterestLevel);
                if (result)
                    return Ok();
                return BadRequest("Failed to add interest");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("interests/{interest}")]
        public async Task<ActionResult> RemoveUserInterest(string interest)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userProfileService.RemoveUserInterestAsync(userId, interest);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpGet("engagement")]
        public async Task<ActionResult<UserEngagementDto>> GetUserEngagement()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var engagement = await _userProfileService.GetUserEngagementAsync(userId);
            return Ok(engagement);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<UserDashboardDto>> GetUserDashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dashboard = await _userProfileService.GetUserDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet("match-summary")]
        public async Task<ActionResult<DemographicMatchSummaryDto>> GetDemographicMatchSummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var summary = await _demographicsRepository.GetDemographicMatchSummaryAsync(userId);
            return Ok(summary);
        }

        [HttpGet("suggested-fields")]
        public async Task<ActionResult<List<string>>> GetSuggestedFieldsToComplete()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var suggestedFields = await _demographicsRepository.GetSuggestedFieldsToCompleteAsync(userId);
            return Ok(suggestedFields);
        }

        [HttpGet("potential-matches")]
        public async Task<ActionResult<int>> GetPotentialMatchingCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var potentialMatches = await _demographicsRepository.GetPotentialMatchingCountAsync(userId);
            return Ok(potentialMatches);
        }

        [HttpPost("refresh-completion")]
        public async Task<ActionResult<UserProfileCompletionDto>> RefreshProfileCompletion()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Force refresh by calling the completion service
            var result = await _userProfileCompletionService.ProcessProfileUpdateAsync(userId, "Manual");
            return Ok(result);
        }

        [HttpGet("debug/{userId}")]
        public async Task<ActionResult<UserProfileCompletionDto>> GetDebugProfileCompletion(string userId)
        {
            // Debug endpoint to see raw profile completion data
            var completion = await _userProfileCompletionService.GetProfileCompletionAsync(userId);
            return Ok(completion);
        }
    }
}