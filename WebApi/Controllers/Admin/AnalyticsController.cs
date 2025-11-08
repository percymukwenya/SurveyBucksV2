using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<PlatformMetricsDto>> GetPlatformMetrics()
        {
            var metrics = await _analyticsService.GetPlatformMetricsAsync();
            return Ok(metrics);
        }

        [HttpGet("top-users")]
        public async Task<ActionResult<IEnumerable<UserAdminDto>>> GetTopUsers([FromQuery] string metric = "points", [FromQuery] int take = 10)
        {
            var users = await _analyticsService.GetTopUsersAsync(metric, take);
            return Ok(users);
        }

        [HttpGet("top-surveys")]
        public async Task<ActionResult<IEnumerable<SurveyAnalyticsSummaryDto>>> GetTopSurveys([FromQuery] string metric = "completions", [FromQuery] int take = 10)
        {
            var surveys = await _analyticsService.GetTopSurveysAsync(metric, take);
            return Ok(surveys);
        }

        [HttpGet("demographics")]
        public async Task<ActionResult<IEnumerable<DemographicBreakdownDto>>> GetDemographicsBreakdown([FromQuery] string type = "gender")
        {
            var breakdown = await _analyticsService.GetUserDemographicsBreakdownAsync(type);
            return Ok(breakdown);
        }

        [HttpGet("timeline")]
        public async Task<ActionResult<IEnumerable<ActivityTimelineDto>>> GetActivityTimeline([FromQuery] int days = 30)
        {
            var timeline = await _analyticsService.GetActivityTimelineAsync(days);
            return Ok(timeline);
        }

        [HttpGet("surveys/{surveyId}/funnel")]
        public async Task<ActionResult<IEnumerable<ConversionFunnelDto>>> GetSurveyConversionFunnel(int surveyId)
        {
            var funnel = await _analyticsService.GetConversionFunnelAsync(surveyId);
            return Ok(funnel);
        }

        [HttpGet("surveys/{surveyId}/export")]
        public async Task<ActionResult> ExportSurveyResponses(int surveyId, [FromQuery] string format = "csv")
        {
            try
            {
                var fileData = await _analyticsService.ExportSurveyResponsesAsync(surveyId, format);

                string contentType = format.ToLower() == "excel" ?
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" :
                    "text/csv";

                string fileName = $"survey_{surveyId}_responses.{(format.ToLower() == "excel" ? "xlsx" : "csv")}";

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
