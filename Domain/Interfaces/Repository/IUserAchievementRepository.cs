using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IUserAchievementRepository
    {
        Task<bool> UnlockAchievementAsync(string userId, string achievementKey, int pointsAwarded);
        Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(string userId);
        Task<bool> HasAchievementAsync(string userId, string achievementKey);
        Task<AchievementDto> GetAchievementByKeyAsync(string achievementKey);
    }
}
