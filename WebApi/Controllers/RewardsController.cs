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
    public class RewardsController : ControllerBase
    {
        private readonly IRewardsService _rewardsService;

        public RewardsController(IRewardsService rewardsService)
        {
            _rewardsService = rewardsService;
        }

        [HttpGet("points")]
        public async Task<ActionResult<UserPointsDto>> GetUserPoints()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var points = await _rewardsService.GetUserPointsAsync(userId);
            return Ok(points);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<PointTransactionDto>>> GetPointTransactions([FromQuery] int take = 20, [FromQuery] int skip = 0)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transactions = await _rewardsService.GetPointTransactionsAsync(userId, take, skip);
            return Ok(transactions);
        }

        [HttpGet("user-rewards")]
        public async Task<ActionResult<IEnumerable<UserRewardDto>>> GetUserRewards()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var rewards = await _rewardsService.GetUserRewardsAsync(userId);
            return Ok(rewards);
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<RewardDto>>> GetAvailableRewards()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var rewards = await _rewardsService.GetAvailableRewardsAsync(userId);
            return Ok(rewards);
        }

        [HttpPost("redeem/{rewardId}")]
        public async Task<ActionResult> RedeemReward(int rewardId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _rewardsService.RedeemRewardAsync(rewardId, userId);
            if (result.Success)
                return Ok();

            return BadRequest("Failed to redeem reward");
        }

        [HttpPost("claim/{userRewardId}")]
        public async Task<ActionResult> ClaimReward(int userRewardId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _rewardsService.ClaimRewardAsync(userRewardId, userId);
            if (result)
                return Ok();
            return BadRequest("Failed to claim reward");
        }
    }
}
