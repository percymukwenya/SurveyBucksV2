using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Request;
using Domain.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileCompletionController : ControllerBase
    {
        private readonly IUserProfileCompletionService _profileCompletionService;
        private readonly IDemographicsRepository _demographicsRepository;
        private readonly IDocumentService _documentService;
        private readonly IBankingService _bankingService;

        public ProfileCompletionController(
            IUserProfileCompletionService profileCompletionService,
            IDemographicsRepository demographicsRepository,
            IDocumentService documentService,
            IBankingService bankingService)
        {
            _profileCompletionService = profileCompletionService;
            _demographicsRepository = demographicsRepository;
            _documentService = documentService;
            _bankingService = bankingService;
        }

        [HttpGet]
        public async Task<ActionResult<UserProfileCompletionDto>> GetProfileCompletion()
        {
            var userId = GetCurrentUserId(); // Your auth implementation
            var completion = await _profileCompletionService.GetProfileCompletionAsync(userId);
            return Ok(completion);
        }

        [HttpGet("detailed")]
        public async Task<ActionResult<UserProfileCompletionDto>> GetDetailedProfileCompletion()
        {
            var userId = GetCurrentUserId();
            var completion = await _profileCompletionService.GetProfileCompletionAsync(userId);
            return Ok(completion);
        }

        [HttpPost("demographics")]
        public async Task<ActionResult<ProfileUpdateResultDto>> UpdateDemographics([FromBody] UpdateDemographicsRequest request)
        {
            var userId = GetCurrentUserId();

            // Map request to DTO with all fields including Income
            var demographicsDto = new DemographicsDto
            {
                UserId = userId,
                Age = request.Age,
                Gender = request.Gender,
                Location = request.Location,
                Country = request.Country,
                State = request.State,
                Income = request.Income,
                IncomeRange = request.IncomeRange,
                HighestEducation = request.HighestEducation,
                EmploymentStatus = request.EmploymentStatus,
                MaritalStatus = request.MaritalStatus,
                HasChildren = request.HasChildren
            };

            var success = await _demographicsRepository.UpdateDemographicsAsync(demographicsDto, userId);

            if (!success)
            {
                return BadRequest("Failed to update demographics");
            }

            // Process profile update and invalidate cache
            var result = await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Demographics");
            return Ok(result);
        }

        [HttpPost("interests")]
        public async Task<ActionResult<ProfileUpdateResultDto>> AddInterest([FromBody] AddInterestRequest request)
        {
            var userId = GetCurrentUserId();

            // Add interest using existing service
            var success = await _demographicsRepository.AddUserInterestAsync(userId, request.Interest, request.InterestLevel);

            if (!success)
            {
                return BadRequest("Failed to add interest");
            }

            // Process profile update
            var result = await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Interests");
            return Ok(result);
        }

        [HttpPost("documents")]
        public async Task<ActionResult<ProfileUpdateResultDto>> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            var userId = GetCurrentUserId();

            // Upload document using existing service
            var document = await _documentService.UploadDocumentAsync(userId, new DocumentUploadRequestDto
            {
                DocumentTypeId = request.DocumentTypeId,
                File = request.File,
                ExpiryDate = request.ExpiryDate
            });

            if (document == null)
            {
                return BadRequest("Failed to upload document");
            }

            // Process profile update
            var result = await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Documents");
            return Ok(result);
        }

        [HttpPost("banking")]
        public async Task<ActionResult<ProfileUpdateResultDto>> AddBankingDetails([FromBody] CreateBankingDetailRequest request)
        {
            var userId = GetCurrentUserId();

            // Add banking details using existing service
            var bankingDetail = await _bankingService.CreateBankingDetailAsync(userId, new CreateBankingDetailDto
            {
                BankName = request.BankName,
                AccountHolderName = request.AccountHolderName,
                AccountNumber = request.AccountNumber,
                AccountType = request.AccountType,
                BranchCode = request.BranchCode,
                IsPrimary = request.IsPrimary
            });

            if (bankingDetail == null)
            {
                return BadRequest("Failed to add banking details");
            }

            // Process profile update
            var result = await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Banking");
            return Ok(result);
        }

        [HttpGet("eligibility")]
        public async Task<ActionResult<ProfileEligibilityDto>> CheckSurveyEligibility()
        {
            var userId = GetCurrentUserId();
            var isEligible = await _profileCompletionService.IsEligibleForSurveysAsync(userId);
            var completion = await _profileCompletionService.GetProfileCompletionAsync(userId);

            return Ok(new ProfileEligibilityDto
            {
                IsEligible = isEligible,
                CompletionPercentage = completion.OverallCompletionPercentage,
                RequiredPercentage = 60, // Minimum for surveys
                NextSteps = completion.NextSteps.Take(3).ToList(),
                Message = isEligible
                    ? "You're ready to participate in surveys!"
                    : $"Complete {60 - completion.OverallCompletionPercentage}% more of your profile to unlock surveys"
            });
        }

        [HttpGet("suggestions")]
        public async Task<ActionResult<List<string>>> GetCompletionSuggestions()
        {
            var userId = GetCurrentUserId();
            var suggestions = await _profileCompletionService.GetProfileCompletionSuggestionsAsync(userId);
            return Ok(suggestions);
        }

        private string GetCurrentUserId()
        {
            // Your authentication implementation
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
