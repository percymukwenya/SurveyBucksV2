using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace WebApi.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve dashboard statistics: {ex.Message}");
            }
        }

        [HttpGet("activity")]
        public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivity([FromQuery] int count = 10)
        {
            try
            {
                var activity = await _dashboardService.GetRecentActivityAsync(count);
                return Ok(activity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve recent activity: {ex.Message}");
            }
        }
    }
}
