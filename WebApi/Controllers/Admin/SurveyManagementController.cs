using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/surveys")]
    public class SurveyManagementController : ControllerBase
    {
        private readonly ISurveyManagementService _surveyManagementService;

        public SurveyManagementController(ISurveyManagementService surveyManagementService)
        {
            _surveyManagementService = surveyManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SurveyAdminListItemDto>>> GetAllSurveys([FromQuery] string status = null)
        {
            var surveys = await _surveyManagementService.GetAllSurveysAsync(status);
            return Ok(surveys);
        }

        [HttpGet("{surveyId}")]
        public async Task<ActionResult<SurveyAdminDetailDto>> GetSurveyDetails(int surveyId)
        {
            var survey = await _surveyManagementService.GetSurveyAdminDetailsAsync(surveyId);
            if (survey == null)
                return NotFound();

            return Ok(survey);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateSurvey(SurveyCreateDto surveyDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var surveyId = await _surveyManagementService.CreateSurveyAsync(surveyDto, userId);
                return CreatedAtAction(nameof(GetSurveyDetails), new { surveyId }, surveyId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{surveyId}")]
        public async Task<ActionResult> UpdateSurvey(int surveyId, SurveyUpdateDto surveyDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (surveyId != surveyDto.Id)
                return BadRequest("Survey ID in URL does not match the one in the request body");

            try
            {
                var result = await _surveyManagementService.UpdateSurveyAsync(surveyDto, userId);
                if (result)
                    return Ok();
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{surveyId}")]
        public async Task<ActionResult> DeleteSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteSurveyAsync(surveyId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpPost("{surveyId}/publish")]
        public async Task<ActionResult> PublishSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.PublishSurveyAsync(surveyId, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to publish survey");
        }

        [HttpPost("{surveyId}/unpublish")]
        public async Task<ActionResult> UnpublishSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.UnpublishSurveyAsync(surveyId, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to unpublish survey");
        }

        [HttpPost("{surveyId}/close")]
        public async Task<ActionResult> CloseSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.CloseSurveyAsync(surveyId, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to close survey");
        }

        [HttpPost("{surveyId}/duplicate")]
        public async Task<ActionResult<int>> DuplicateSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var newSurveyId = await _surveyManagementService.DuplicateSurveyAsync(surveyId, userId);
                return CreatedAtAction(nameof(GetSurveyDetails), new { surveyId = newSurveyId }, newSurveyId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{surveyId}/analytics")]
        public async Task<ActionResult<SurveyAnalyticsDto>> GetSurveyAnalytics(int surveyId)
        {
            var analytics = await _surveyManagementService.GetSurveyAnalyticsAsync(surveyId);
            if (analytics == null)
                return NotFound();

            return Ok(analytics);
        }

        [HttpGet("{surveyId}/targets/age-ranges")]
        public async Task<ActionResult<IEnumerable<AgeRangeTargetDto>>> GetSurveyAgeRangeTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyAgeRangeTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/age-ranges")]
        public async Task<ActionResult<int>> AddAgeRangeTarget(AgeRangeTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddAgeRangeTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/age-ranges/{targetId}")]
        public async Task<ActionResult> DeleteAgeRangeTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteAgeRangeTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Gender Targets
        [HttpGet("{surveyId}/targets/genders")]
        public async Task<ActionResult<IEnumerable<GenderTargetDto>>> GetSurveyGenderTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyGenderTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/genders")]
        public async Task<ActionResult<int>> AddGenderTarget(GenderTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddGenderTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/genders/{targetId}")]
        public async Task<ActionResult> DeleteGenderTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteGenderTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Education Targets
        [HttpGet("{surveyId}/targets/education")]
        public async Task<ActionResult<IEnumerable<EducationTargetDto>>> GetSurveyEducationTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyEducationTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/education")]
        public async Task<ActionResult<int>> AddEducationTarget(EducationTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddEducationTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/education/{targetId}")]
        public async Task<ActionResult> DeleteEducationTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteEducationTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Income Range Targets
        [HttpGet("{surveyId}/targets/income-ranges")]
        public async Task<ActionResult<IEnumerable<IncomeRangeTargetDto>>> GetSurveyIncomeRangeTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyIncomeRangeTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/income-ranges")]
        public async Task<ActionResult<int>> AddIncomeRangeTarget(IncomeRangeTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddIncomeRangeTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/income-ranges/{targetId}")]
        public async Task<ActionResult> DeleteIncomeRangeTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteIncomeRangeTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Location Targets
        [HttpGet("{surveyId}/targets/locations")]
        public async Task<ActionResult<IEnumerable<LocationTargetDto>>> GetSurveyLocationTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyLocationTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/locations")]
        public async Task<ActionResult<int>> AddLocationTarget(LocationTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddLocationTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/locations/{targetId}")]
        public async Task<ActionResult> DeleteLocationTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteLocationTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Country Targets
        [HttpGet("{surveyId}/targets/countries")]
        public async Task<ActionResult<IEnumerable<CountryTargetDto>>> GetSurveyCountryTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyCountryTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/countries")]
        public async Task<ActionResult<int>> AddCountryTarget(CountryTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddCountryTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/countries/{targetId}")]
        public async Task<ActionResult> DeleteCountryTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteCountryTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // State Targets
        [HttpGet("{surveyId}/targets/states")]
        public async Task<ActionResult<IEnumerable<StateTargetDto>>> GetSurveyStateTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyStateTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/states")]
        public async Task<ActionResult<int>> AddStateTarget(StateTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddStateTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/states/{targetId}")]
        public async Task<ActionResult> DeleteStateTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteStateTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Household Size Targets
        [HttpGet("{surveyId}/targets/household-sizes")]
        public async Task<ActionResult<IEnumerable<HouseholdSizeTargetDto>>> GetSurveyHouseholdSizeTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyHouseholdSizeTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/household-sizes")]
        public async Task<ActionResult<int>> AddHouseholdSizeTarget(HouseholdSizeTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddHouseholdSizeTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/household-sizes/{targetId}")]
        public async Task<ActionResult> DeleteHouseholdSizeTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteHouseholdSizeTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Parental Status Targets
        [HttpGet("{surveyId}/targets/parental-status")]
        public async Task<ActionResult<IEnumerable<ParentalStatusTargetDto>>> GetSurveyParentalStatusTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyParentalStatusTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/parental-status")]
        public async Task<ActionResult<int>> AddParentalStatusTarget(ParentalStatusTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddParentalStatusTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/parental-status/{targetId}")]
        public async Task<ActionResult> DeleteParentalStatusTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteParentalStatusTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Industry Targets
        [HttpGet("{surveyId}/targets/industries")]
        public async Task<ActionResult<IEnumerable<IndustryTargetDto>>> GetSurveyIndustryTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyIndustryTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/industries")]
        public async Task<ActionResult<int>> AddIndustryTarget(IndustryTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddIndustryTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/industries/{targetId}")]
        public async Task<ActionResult> DeleteIndustryTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteIndustryTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Occupation Targets
        [HttpGet("{surveyId}/targets/occupations")]
        public async Task<ActionResult<IEnumerable<OccupationTargetDto>>> GetSurveyOccupationTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyOccupationTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/occupations")]
        public async Task<ActionResult<int>> AddOccupationTarget(OccupationTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddOccupationTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/occupations/{targetId}")]
        public async Task<ActionResult> DeleteOccupationTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteOccupationTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Marital Status Targets
        [HttpGet("{surveyId}/targets/marital-status")]
        public async Task<ActionResult<IEnumerable<MaritalStatusTargetDto>>> GetSurveyMaritalStatusTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyMaritalStatusTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/marital-status")]
        public async Task<ActionResult<int>> AddMaritalStatusTarget(MaritalStatusTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddMaritalStatusTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/marital-status/{targetId}")]
        public async Task<ActionResult> DeleteMaritalStatusTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteMaritalStatusTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Interest Targets
        [HttpGet("{surveyId}/targets/interests")]
        public async Task<ActionResult<IEnumerable<InterestTargetDto>>> GetSurveyInterestTargets(int surveyId)
        {
            var targets = await _surveyManagementService.GetSurveyInterestTargetsAsync(surveyId);
            return Ok(targets);
        }

        [HttpPost("targets/interests")]
        public async Task<ActionResult<int>> AddInterestTarget(InterestTargetDto target)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var targetId = await _surveyManagementService.AddInterestTargetAsync(target, userId);
                return Ok(targetId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("targets/interests/{targetId}")]
        public async Task<ActionResult> DeleteInterestTarget(int targetId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _surveyManagementService.DeleteInterestTargetAsync(targetId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Get All Targets - convenience method for the UI
        [HttpGet("{surveyId}/targets/all")]
        public async Task<ActionResult<SurveyTargetsDto>> GetAllSurveyTargets(int surveyId)
        {
            // Create a data structure to hold all targeting criteria
            var allTargets = new SurveyTargetsDto
            {
                SurveyId = surveyId,
                AgeRangeTargets = await _surveyManagementService.GetSurveyAgeRangeTargetsAsync(surveyId),
                GenderTargets = await _surveyManagementService.GetSurveyGenderTargetsAsync(surveyId),
                EducationTargets = await _surveyManagementService.GetSurveyEducationTargetsAsync(surveyId),
                IncomeRangeTargets = await _surveyManagementService.GetSurveyIncomeRangeTargetsAsync(surveyId),
                LocationTargets = await _surveyManagementService.GetSurveyLocationTargetsAsync(surveyId),
                CountryTargets = await _surveyManagementService.GetSurveyCountryTargetsAsync(surveyId),
                StateTargets = await _surveyManagementService.GetSurveyStateTargetsAsync(surveyId),
                HouseholdSizeTargets = await _surveyManagementService.GetSurveyHouseholdSizeTargetsAsync(surveyId),
                ParentalStatusTargets = await _surveyManagementService.GetSurveyParentalStatusTargetsAsync(surveyId),
                IndustryTargets = await _surveyManagementService.GetSurveyIndustryTargetsAsync(surveyId),
                OccupationTargets = await _surveyManagementService.GetSurveyOccupationTargetsAsync(surveyId),
                MaritalStatusTargets = await _surveyManagementService.GetSurveyMaritalStatusTargetsAsync(surveyId),
                InterestTargets = await _surveyManagementService.GetSurveyInterestTargetsAsync(surveyId)
            };

            return Ok(allTargets);
        }
    }
}

public class SurveyTargetsDto
{
    public int SurveyId { get; set; }
    public IEnumerable<AgeRangeTargetDto> AgeRangeTargets { get; set; } = new List<AgeRangeTargetDto>();
    public IEnumerable<GenderTargetDto> GenderTargets { get; set; } = new List<GenderTargetDto>();
    public IEnumerable<EducationTargetDto> EducationTargets { get; set; } = new List<EducationTargetDto>();
    public IEnumerable<IncomeRangeTargetDto> IncomeRangeTargets { get; set; } = new List<IncomeRangeTargetDto>();
    public IEnumerable<LocationTargetDto> LocationTargets { get; set; } = new List<LocationTargetDto>();
    public IEnumerable<CountryTargetDto> CountryTargets { get; set; } = new List<CountryTargetDto>();
    public IEnumerable<StateTargetDto> StateTargets { get; set; } = new List<StateTargetDto>();
    public IEnumerable<HouseholdSizeTargetDto> HouseholdSizeTargets { get; set; } = new List<HouseholdSizeTargetDto>();
    public IEnumerable<ParentalStatusTargetDto> ParentalStatusTargets { get; set; } = new List<ParentalStatusTargetDto>();
    public IEnumerable<IndustryTargetDto> IndustryTargets { get; set; } = new List<IndustryTargetDto>();
    public IEnumerable<OccupationTargetDto> OccupationTargets { get; set; } = new List<OccupationTargetDto>();
    public IEnumerable<MaritalStatusTargetDto> MaritalStatusTargets { get; set; } = new List<MaritalStatusTargetDto>();
    public IEnumerable<InterestTargetDto> InterestTargets { get; set; } = new List<InterestTargetDto>();
}
