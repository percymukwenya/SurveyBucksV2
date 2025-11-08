using Application.Services;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyParticipationService _surveyService;
        private readonly ISurveyResponseService _responseService;
        private readonly ISurveyParticipationRepository _surveyParticipationRepository;
        private readonly IGamificationService _gamificationService;
        private readonly ISurveyAccessService _surveyAccessService;

        public SurveyController(
            ISurveyParticipationService surveyService, 
            ISurveyResponseService responseService, ISurveyParticipationRepository surveyParticipationRepository,
            IGamificationService gamificationService, ISurveyAccessService surveyAccessService)
        {
            _surveyService = surveyService;
            _responseService = responseService;
            _surveyParticipationRepository = surveyParticipationRepository;
            _gamificationService = gamificationService;
            _surveyAccessService = surveyAccessService;
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<SurveyListItemDto>>> GetAvailableSurveys()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var accessResult = await _surveyAccessService.GetUserSurveyAccessAsync(userId);

            if (!accessResult.HasAccess)
            {
                return Ok(new
                {
                    hasAccess = false,
                    completionPercentage = accessResult.CompletionPercentage,
                    message = accessResult.Message,
                    blockingFactors = accessResult.BlockingFactors,
                    incompleteSections = accessResult.IncompleteSections,
                    surveys = new List<SurveyListItemDto>() // Empty list
                });
            }

            return Ok(new
            {
                hasAccess = true,
                completionPercentage = 100,
                message = accessResult.Message,
                surveys = accessResult.AvailableSurveys,
                totalCount = accessResult.TotalAvailableSurveys
            });
        }

        [HttpGet("{surveyId}")]
        public async Task<ActionResult<SurveyDetailDto>> GetSurveyDetails(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var survey = await _surveyService.GetSurveyDetailsAsync(surveyId, userId);
                return Ok(survey);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{surveyId}/details")]
        public async Task<ActionResult<SurveyDetailDto>> GetSimpleSurveyDetails(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                var survey = await _surveyService.GetSurveyDetailsAsync(surveyId, userId);

                // Track survey view for analytics
                //await _gamificationService.ProcessChallengeProgressAsync(userId, "ViewSurvey", 1);

                return Ok(survey);
            }
            catch (NotFoundException)
            {
                return NotFound($"Survey {surveyId} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("You are not authorized to view this survey");
            }
        }

        [HttpGet("{surveyId}/progress")]
        public async Task<ActionResult<SurveyProgressDto>> GetSurveyProgress(int surveyId)
        {
            var userId = GetCurrentUserId();

            try
            {
                var progress = await _surveyParticipationRepository.GetSurveyProgressAsync(surveyId, userId);
                return Ok(progress);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{surveyId}/navigation")]
        public async Task<ActionResult<NavigationDto>> GetSurveyNavigation(int surveyId)
        {
            var userId = GetCurrentUserId();

            var navigation = await _surveyParticipationRepository.GetSurveyNavigationAsync(surveyId, userId);
            return Ok(navigation);
        }

        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<SurveySectionDetailDto>> GetSectionDetails(int sectionId)
        {
            var userId = GetCurrentUserId();

            try
            {
                var section = await _surveyParticipationRepository.GetSectionWithQuestionsAsync(sectionId, userId);
                if (section == null)
                    return NotFound($"Section {sectionId} not found");

                return Ok(section);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("You are not authorized to view this section");
            }
        }

        [HttpPost("response")]
        public async Task<ActionResult<ResponseValidationResult>> SaveResponse(SurveyResponseDto response)
        {
            var userId = GetCurrentUserId();

            try
            {
                var result = await _responseService.ValidateAndSaveResponseAsync(response, userId);

                if (!result.IsValid)
                {
                    return BadRequest(result);
                }

                // Handle screening responses
                //if (result.IsScreeningResponse && !result.ScreeningResult.IsQualified)
                //{
                //    await _surveyService.DisqualifyParticipationAsync(response.SurveyParticipationId,
                //        result.ScreeningResult.DisqualificationReason, userId);

                //    return Ok(new
                //    {
                //        Success = true,
                //        Disqualified = true,
                //        Reason = result.ScreeningResult.DisqualificationReason
                //    });
                //}

                // Handle conditional logic
                if (result.NextAction?.ActionType != null)
                {
                    return Ok(new
                    {
                        Success = true,
                        NextAction = result.NextAction
                    });
                }

                return Ok(new { Success = true, ResponseId = result.ResponseId });
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("responses/batch")]
        public async Task<ActionResult<BatchResponseResult>> SaveMultipleResponses(List<SurveyResponseDto> responses)
        {
            var userId = GetCurrentUserId();

            try
            {
                var result = await _responseService.SaveMultipleResponsesAsync(responses, userId);

                if (result.FailedResponses.Any())
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error saving batch responses for user {UserId}", userId);
                return StatusCode(500, "An error occurred while saving responses");
            }
        }

        [HttpPost("{surveyId}/enroll")]
        public async Task<ActionResult<SurveyParticipationDto>> EnrollInSurvey(int surveyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var participation = await _surveyService.EnrollInSurveyAsync(userId, surveyId);
                return Ok(participation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("participation/{participationId}")]
        public async Task<ActionResult<SurveyParticipationDto>> GetParticipation(int participationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var participation = await _surveyService.GetParticipationAsync(participationId, userId);
                return Ok(participation);
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

        [HttpPut("participation/progress")]
        public async Task<ActionResult> UpdateProgress(SurveyProgressUpdateDto progressDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _surveyService.UpdateParticipationProgressAsync(progressDto, userId);
                if (result)
                    return Ok();
                return BadRequest("Failed to update progress");
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("participation/{participationId}/complete")]
        public async Task<ActionResult> CompleteSurvey(int participationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _surveyService.CompleteSurveyAsync(participationId, userId);
                if (result)
                    return Ok();
                return BadRequest("Failed to complete survey");
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

        [HttpGet("participation")]
        public async Task<ActionResult<IEnumerable<SurveyParticipationSummaryDto>>> GetUserParticipations([FromQuery] string status = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var participations = await _surveyService.GetUserParticipationsAsync(userId, status);
            return Ok(participations);
        }
         
        //[HttpPost("response")]
        //public async Task<ActionResult> SaveSurveyResponse(SurveyResponseDto response)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    try
        //    {
        //        var result = await _surveyService.SaveSurveyResponseAsync(response, userId);
        //        if (result)
        //            return Ok();
        //        return BadRequest("Failed to save response");
        //    }
        //    catch (NotFoundException)
        //    {
        //        return NotFound();
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return Forbid();
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpGet("participation/{participationId}/responses")]
        public async Task<ActionResult<IEnumerable<SurveyResponseDto>>> GetSavedResponses(int participationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var responses = await _surveyService.GetSavedResponsesAsync(participationId, userId);
                return Ok(responses);
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

        [HttpPost("feedback")]
        public async Task<ActionResult> SubmitSurveyFeedback(SurveyFeedbackDto feedback)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _surveyService.SubmitSurveyFeedbackAsync(feedback, userId);
                if (result)
                    return Ok();
                return BadRequest("Failed to submit feedback");
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }
    }
}
