using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace WebApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/questions/logic")]
    public class QuestionLogicController : ControllerBase
    {
        private readonly IQuestionLogicService _logicService;

        public QuestionLogicController(IQuestionLogicService logicService)
        {
            _logicService = logicService;
        }

        [HttpGet("question/{questionId}")]
        public async Task<ActionResult<IEnumerable<QuestionLogicDto>>> GetQuestionLogic(int questionId)
        {
            var logic = await _logicService.GetQuestionLogicAsync(questionId);
            return Ok(logic);
        }

        [HttpGet("survey/{surveyId}")]
        public async Task<ActionResult<IEnumerable<QuestionLogicDto>>> GetSurveyLogic(int surveyId)
        {
            var logic = await _logicService.GetSurveyLogicAsync(surveyId);
            return Ok(logic);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateLogic(QuestionLogicCreateDto logicDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var logicId = await _logicService.CreateQuestionLogicAsync(logicDto, userId);
                return Ok(logicId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{logicId}")]
        public async Task<ActionResult> UpdateLogic(int logicId, QuestionLogicUpdateDto logicDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (logicId != logicDto.Id)
                return BadRequest("Logic ID mismatch");

            try
            {
                var result = await _logicService.UpdateQuestionLogicAsync(logicDto, userId);
                if (result)
                    return Ok();
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{logicId}")]
        public async Task<ActionResult> DeleteLogic(int logicId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _logicService.DeleteQuestionLogicAsync(logicId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpGet("survey/{surveyId}/validate")]
        public async Task<ActionResult<bool>> ValidateSurveyLogic(int surveyId)
        {
            var isValid = await _logicService.ValidateSurveyLogicAsync(surveyId);
            return Ok(new { isValid });
        }

        [HttpGet("survey/{surveyId}/flow")]
        public async Task<ActionResult<SurveyFlowVisualizationDto>> GetSurveyFlow(int surveyId)
        {
            var flow = await _logicService.GetSurveyFlowVisualizationAsync(surveyId);
            return Ok(flow);
        }
    }
}
