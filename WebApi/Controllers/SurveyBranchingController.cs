using Application.Services;
using Domain.Interfaces.Service;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/survey-branching")]
    [Authorize]
    public class SurveyBranchingController : ControllerBase
    {
        private readonly ISurveyBranchingService _branchingService;
        private readonly ILogger<SurveyBranchingController> _logger;

        public SurveyBranchingController(
            ISurveyBranchingService branchingService,
            ILogger<SurveyBranchingController> logger)
        {
            _branchingService = branchingService;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates branching logic for a specific question response
        /// </summary>
        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateQuestionLogic([FromBody] BranchingEvaluationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _branchingService.EvaluateQuestionLogicAsync(
                    request.QuestionId, 
                    request.ResponseValue, 
                    request.ParticipationId);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                return Ok(new
                {
                    hasActions = result.HasActions,
                    actions = result.Actions.Select(a => new
                    {
                        actionType = a.ActionType.ToString(),
                        targetQuestionId = a.TargetQuestionId,
                        targetQuestionIds = a.TargetQuestionIds,
                        targetSectionId = a.TargetSectionId,
                        message = a.Message,
                        metadata = a.Metadata
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating branching logic");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Gets the current flow state for a participation
        /// </summary>
        [HttpGet("flow-state/{participationId}")]
        public async Task<IActionResult> GetFlowState(int participationId)
        {
            try
            {
                var flowState = await _branchingService.GetCurrentFlowStateAsync(participationId);
                
                return Ok(new
                {
                    participationId = flowState.ParticipationId,
                    surveyId = flowState.SurveyId,
                    currentSectionId = flowState.CurrentSectionId,
                    currentQuestionId = flowState.CurrentQuestionId,
                    completedQuestions = flowState.CompletedQuestions,
                    availableQuestions = flowState.AvailableQuestions,
                    conditionalPath = flowState.ConditionalPath.Select(p => new
                    {
                        questionId = p.QuestionId,
                        response = p.Response,
                        actionTaken = p.ActionTaken,
                        timestamp = p.Timestamp
                    }),
                    isComplete = flowState.IsComplete
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow state for participation {ParticipationId}", participationId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Gets available questions for a specific section based on current flow state
        /// </summary>
        [HttpGet("available-questions/{participationId}/{sectionId}")]
        public async Task<IActionResult> GetAvailableQuestions(int participationId, int sectionId)
        {
            try
            {
                var availableQuestions = await _branchingService.GetAvailableQuestionsAsync(participationId, sectionId);
                return Ok(new { availableQuestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available questions for participation {ParticipationId}, section {SectionId}", 
                    participationId, sectionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Processes branching logic after a response is submitted
        /// </summary>
        [HttpPost("process-response")]
        public async Task<IActionResult> ProcessResponseBranching([FromBody] ResponseBranchingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = new SurveyResponseDto
                {
                    QuestionId = request.QuestionId,
                    Answer = request.Answer,
                    SurveyParticipationId = request.ParticipationId
                };

                var action = await _branchingService.ProcessResponseBranchingAsync(response, request.ParticipationId);

                return Ok(new
                {
                    actionType = action.ActionType.ToString(),
                    targetQuestionId = action.TargetQuestionId,
                    targetQuestionIds = action.TargetQuestionIds,
                    targetSectionId = action.TargetSectionId,
                    message = action.Message,
                    metadata = action.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing response branching");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validates the integrity of survey flow logic
        /// </summary>
        [HttpGet("validate-flow/{surveyId}")]
        [Authorize(Policy = "AdminOnly")] // Only admins should access this
        public async Task<IActionResult> ValidateFlowIntegrity(int surveyId)
        {
            try
            {
                var isValid = await _branchingService.ValidateSurveyFlowIntegrityAsync(surveyId);
                
                return Ok(new 
                { 
                    surveyId, 
                    isValid,
                    message = isValid ? "Survey flow is valid" : "Survey flow has integrity issues"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating flow integrity for survey {SurveyId}", surveyId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generates a visual flow map for a survey
        /// </summary>
        [HttpGet("flow-map/{surveyId}")]
        [Authorize(Policy = "AdminOnly")] // Only admins should access this
        public async Task<IActionResult> GenerateFlowMap(int surveyId)
        {
            try
            {
                var flowMap = await _branchingService.GenerateFlowMapAsync(surveyId);
                
                return Ok(new
                {
                    surveyId = flowMap.SurveyId,
                    nodes = flowMap.Nodes.Select(n => new
                    {
                        id = n.Id,
                        type = n.Type,
                        label = n.Label,
                        properties = n.Properties
                    }),
                    edges = flowMap.Edges.Select(e => new
                    {
                        fromNodeId = e.FromNodeId,
                        toNodeId = e.ToNodeId,
                        condition = e.Condition,
                        label = e.Label
                    }),
                    decisionPoints = flowMap.DecisionPoints.Select(dp => new
                    {
                        questionId = dp.QuestionId,
                        conditions = dp.Conditions,
                        actions = dp.Actions.Select(a => new
                        {
                            actionType = a.ActionType.ToString(),
                            targetQuestionId = a.TargetQuestionId,
                            targetSectionId = a.TargetSectionId,
                            message = a.Message
                        })
                    }),
                    endPoints = flowMap.EndPoints,
                    orphanedQuestions = flowMap.OrphanedQuestions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating flow map for survey {SurveyId}", surveyId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Real-time validation of branching logic as admin configures it
        /// </summary>
        [HttpPost("validate-logic")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ValidateLogicRule([FromBody] LogicValidationRequest request)
        {
            try
            {
                // Validate the logic rule without saving it
                var issues = ValidateLogicRuleStructure(request);
                
                return Ok(new
                {
                    isValid = !issues.Any(),
                    issues = issues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating logic rule");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private List<string> ValidateLogicRuleStructure(LogicValidationRequest request)
        {
            var issues = new List<string>();

            // Validate condition type
            var validConditionTypes = new[] { "equals", "not_equals", "contains", "greater_than", "less_than", "between", "in_list", "regex_match", "cross_question" };
            if (!validConditionTypes.Contains(request.ConditionType?.ToLower()))
            {
                issues.Add($"Invalid condition type: {request.ConditionType}");
            }

            // Validate action type
            var validActionTypes = new[] { "show_question", "hide_question", "show_questions", "jump_to_section", "skip_to_question", "end_survey", "disqualify" };
            if (!validActionTypes.Contains(request.ActionType?.ToLower()))
            {
                issues.Add($"Invalid action type: {request.ActionType}");
            }

            // Validate condition value requirements
            if (request.ConditionType?.ToLower() == "between" && string.IsNullOrWhiteSpace(request.ConditionValue2))
            {
                issues.Add("Between condition requires two values");
            }

            // Validate target requirements
            var actionType = request.ActionType?.ToLower();
            if (actionType == "show_question" || actionType == "hide_question" || actionType == "skip_to_question")
            {
                if (!request.TargetQuestionId.HasValue)
                {
                    issues.Add($"Action type '{request.ActionType}' requires a target question ID");
                }
            }
            else if (actionType == "jump_to_section")
            {
                if (!request.TargetSectionId.HasValue)
                {
                    issues.Add($"Action type '{request.ActionType}' requires a target section ID");
                }
            }
            else if (actionType == "show_questions")
            {
                if (request.TargetQuestionIds == null || !request.TargetQuestionIds.Any())
                {
                    issues.Add($"Action type '{request.ActionType}' requires target question IDs");
                }
            }

            return issues;
        }
    }

    #region Request Models

    public class BranchingEvaluationRequest
    {
        public int QuestionId { get; set; }
        public string ResponseValue { get; set; } = string.Empty;
        public int ParticipationId { get; set; }
    }

    public class ResponseBranchingRequest
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
        public int ParticipationId { get; set; }
    }

    public class LogicValidationRequest
    {
        public int QuestionId { get; set; }
        public string LogicType { get; set; } = string.Empty;
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionValue { get; set; } = string.Empty;
        public string? ConditionValue2 { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public int? TargetQuestionId { get; set; }
        public List<int>? TargetQuestionIds { get; set; }
        public int? TargetSectionId { get; set; }
        public string? Message { get; set; }
        public int Order { get; set; }
    }

    #endregion
}