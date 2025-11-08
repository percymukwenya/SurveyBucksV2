using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/questions")]
    public class QuestionManagementController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionManagementController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<QuestionTypeDto>>> GetQuestionTypes()
        {
            var types = await _questionService.GetQuestionTypesAsync();
            return Ok(types);
        }

        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetSectionQuestions(int sectionId)
        {
            var questions = await _questionService.GetSectionQuestionsAsync(sectionId);
            return Ok(questions);
        }

        [HttpGet("{questionId}")]
        public async Task<ActionResult<QuestionDetailDto>> GetQuestionDetails(int questionId)
        {
            var question = await _questionService.GetQuestionDetailsAsync(questionId);
            if (question == null)
                return NotFound();

            return Ok(question);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateQuestion(QuestionCreateDto questionDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var questionId = await _questionService.CreateQuestionAsync(questionDto, userId);
                return CreatedAtAction(nameof(GetQuestionDetails), new { questionId }, questionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{questionId}")]
        public async Task<ActionResult> UpdateQuestion(int questionId, QuestionUpdateDto questionDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (questionId != questionDto.Id)
                return BadRequest("Question ID in URL does not match the one in the request body");

            try
            {
                var result = await _questionService.UpdateQuestionAsync(questionDto, userId);
                if (result)
                    return Ok();
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{questionId}")]
        public async Task<ActionResult> DeleteQuestion(int questionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _questionService.DeleteQuestionAsync(questionId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderQuestions(QuestionReorderRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.SectionId <= 0)
                return BadRequest("Invalid section ID");

            if (request.QuestionOrders == null || !request.QuestionOrders.Any())
                return BadRequest("No question orders provided");

            var result = await _questionService.ReorderQuestionsAsync(request.SectionId, request.QuestionOrders, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to reorder questions");
        }

        // Response Choices endpoints
        [HttpGet("{questionId}/choices")]
        public async Task<ActionResult<IEnumerable<QuestionResponseChoiceDto>>> GetQuestionChoices(int questionId)
        {
            var choices = await _questionService.GetQuestionChoicesAsync(questionId);
            return Ok(choices);
        }

        [HttpPost("choices")]
        public async Task<ActionResult<int>> AddQuestionChoice(QuestionChoiceCreateDto choiceDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var choiceId = await _questionService.AddQuestionChoiceAsync(choiceDto, userId);
                return Ok(choiceId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("choices/{choiceId}")]
        public async Task<ActionResult> UpdateQuestionChoice(int choiceId, QuestionChoiceUpdateDto choiceDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (choiceId != choiceDto.Id)
                return BadRequest("Choice ID in URL does not match the one in the request body");

            try
            {
                var result = await _questionService.UpdateQuestionChoiceAsync(choiceDto, userId);
                if (result)
                    return Ok();
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("choices/{choiceId}")]
        public async Task<ActionResult> DeleteQuestionChoice(int choiceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _questionService.DeleteQuestionChoiceAsync(choiceId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        // Matrix rows and columns endpoints
        [HttpGet("{questionId}/matrix/rows")]
        public async Task<ActionResult<IEnumerable<MatrixRowDto>>> GetMatrixRows(int questionId)
        {
            var rows = await _questionService.GetMatrixRowsAsync(questionId);
            return Ok(rows);
        }

        [HttpGet("{questionId}/matrix/columns")]
        public async Task<ActionResult<IEnumerable<MatrixColumnDto>>> GetMatrixColumns(int questionId)
        {
            var columns = await _questionService.GetMatrixColumnsAsync(questionId);
            return Ok(columns);
        }

        [HttpPost("matrix/rows")]
        public async Task<ActionResult<int>> AddMatrixRow(MatrixRowDto rowDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var rowId = await _questionService.AddMatrixRowAsync(rowDto, userId);
                return Ok(rowId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("matrix/columns")]
        public async Task<ActionResult<int>> AddMatrixColumn(MatrixColumnDto columnDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var columnId = await _questionService.AddMatrixColumnAsync(columnDto, userId);
                return Ok(columnId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("matrix/rows/{rowId}")]
        public async Task<ActionResult> DeleteMatrixRow(int rowId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _questionService.DeleteMatrixRowAsync(rowId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpDelete("matrix/columns/{columnId}")]
        public async Task<ActionResult> DeleteMatrixColumn(int columnId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _questionService.DeleteMatrixColumnAsync(columnId, userId);
            if (result)
                return Ok();
            return NotFound();
        }
    }
}
