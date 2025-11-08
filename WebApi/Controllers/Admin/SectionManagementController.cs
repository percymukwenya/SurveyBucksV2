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
    [Route("api/admin/sections")]
    public class SectionManagementController : ControllerBase
    {
        private readonly ISurveySectionService _sectionService;

        public SectionManagementController(ISurveySectionService sectionService)
        {
            _sectionService = sectionService;
        }

        [HttpGet("survey/{surveyId}")]
        public async Task<ActionResult<IEnumerable<SurveySectionDto>>> GetSurveySections(int surveyId)
        {
            var sections = await _sectionService.GetSurveySectionsAsync(surveyId);
            return Ok(sections);
        }

        [HttpGet("{sectionId}")]
        public async Task<ActionResult<SurveySectionDto>> GetSectionById(int sectionId)
        {
            var section = await _sectionService.GetSectionByIdAsync(sectionId);
            if (section == null)
                return NotFound();

            return Ok(section);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateSection(SurveySectionCreateDto sectionDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var sectionId = await _sectionService.CreateSectionAsync(sectionDto, userId);
                return CreatedAtAction(nameof(GetSectionById), new { sectionId }, sectionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{sectionId}")]
        public async Task<ActionResult> UpdateSection(int sectionId, SurveySectionUpdateDto sectionDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (sectionId != sectionDto.Id)
                return BadRequest("Section ID in URL does not match the one in the request body");

            try
            {
                var result = await _sectionService.UpdateSectionAsync(sectionDto, userId);
                if (result)
                    return Ok();
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{sectionId}")]
        public async Task<ActionResult> DeleteSection(int sectionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _sectionService.DeleteSectionAsync(sectionId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderSections(SectionReorderRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.SurveyId <= 0)
                return BadRequest("Invalid survey ID");

            if (request.SectionOrders == null || !request.SectionOrders.Any())
                return BadRequest("No section orders provided");

            var result = await _sectionService.ReorderSectionsAsync(request.SurveyId, request.SectionOrders, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to reorder sections");
        }
    }
}
