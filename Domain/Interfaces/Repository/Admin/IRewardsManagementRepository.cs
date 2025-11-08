using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IRewardsManagementRepository
    {
        Task<IEnumerable<RewardAdminDto>> GetAllRewardsAsync(bool activeOnly = false);
        Task<RewardDetailAdminDto> GetRewardDetailsAsync(int rewardId);
        Task<int> CreateRewardAsync(RewardCreateDto reward, string createdBy);
        Task<bool> UpdateRewardAsync(RewardUpdateDto reward, string modifiedBy);
        Task<bool> DeleteRewardAsync(int rewardId, string deletedBy);
        Task<IEnumerable<UserRewardAdminDto>> GetPendingRedemptionsAsync();
        Task<bool> ProcessRedemptionAsync(int userRewardId, string status, string processedBy);
    }
}
