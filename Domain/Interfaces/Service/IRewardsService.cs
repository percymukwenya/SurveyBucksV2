using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface IRewardsService
    {
        Task<RewardRedemptionResultDto> RedeemRewardAsync(int rewardId, string userId);
        Task<bool> ClaimRewardAsync(int userRewardId, string userId);

        Task<UserPointsDto> GetUserPointsAsync(string userId);
        Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(string userId, int take = 20, int skip = 0);
        Task<IEnumerable<UserRewardDto>> GetUserRewardsAsync(string userId);        
        Task<IEnumerable<RewardDto>> GetAvailableRewardsAsync(string userId);
    }
}
