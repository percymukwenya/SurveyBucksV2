using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IRewardsRepository
    {
        // Core CRUD operations only
        Task<UserPointsDto> GetUserPointsAsync(string userId);
        Task<RewardDto> GetRewardByIdAsync(int rewardId);
        Task<int> CreateUserRewardAsync(UserRewardDto userReward);
        Task<bool> DeductUserPointsAsync(string userId, int pointsToDeduct, string reason, string referenceId);
        Task<bool> CreatePointTransactionAsync(PointTransactionDto transaction);
        Task<bool> DecrementRewardQuantityAsync(int rewardId, string modifiedBy);
        Task<int?> GetCompletedParticipationForSurveyAsync(string userId, int surveyId);
        Task<bool> ClaimUserRewardAsync(int userRewardId, string userId);
        Task<UserRewardDto> GetUserRewardByIdAsync(int userRewardId);

        // Query operations
        Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(string userId, int take = 20, int skip = 0);
        Task<IEnumerable<UserRewardDto>> GetUserRewardsAsync(string userId);
        Task<IEnumerable<RewardDto>> GetAvailableRewardsAsync(string userId);

        Task<bool> RedeemRewardAsync(int rewardId, string userId);
        Task<bool> ClaimRewardAsync(int userRewardId, string userId);
        

        
    }
}
