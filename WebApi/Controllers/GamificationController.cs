using Domain.Interfaces.Service;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;

        public GamificationController(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetUserStats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var stats = await _gamificationService.GetUserStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("level")]
        public async Task<ActionResult<UserLevelDto>> GetUserLevel()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var level = await _gamificationService.GetUserLevelAsync(userId);
            return Ok(level);
        }

        [HttpGet("achievements")]
        public async Task<ActionResult<IEnumerable<UserAchievementDto>>> GetUserAchievements()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var achievements = await _gamificationService.GetUserAchievementsAsync(userId);
            return Ok(achievements);
        }

        [HttpGet("challenges")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetActiveChallenges()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var challenges = await _gamificationService.GetActiveChallengesAsync(userId);
            return Ok(challenges);
        }

        [HttpGet("leaderboards")]
        public async Task<ActionResult<IEnumerable<LeaderboardSummaryDto>>> GetAvailableLeaderboards()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var leaderboards = await _gamificationService.GetAvailableLeaderboardsAsync(userId);
            return Ok(leaderboards);
        }

        [HttpGet("leaderboards/{leaderboardId}")]
        public async Task<ActionResult<LeaderboardDto>> GetLeaderboard(int leaderboardId, [FromQuery] int top = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var leaderboard = await _gamificationService.GetLeaderboardAsync(leaderboardId, userId, top);
            if (leaderboard == null)
                return NotFound();

            return Ok(leaderboard);
        }
    }
}
