using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IGamificationRepository
    {
        Task<UserEngagementDto> GetUserEngagementAsync(string userId);
        Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId);
        Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10);
        Task<UserLevelDto> GetUserLevelAsync(string userId);
        Task<bool> CheckForAchievementsAsync(string userId);
        Task<bool> ProcessChallengeProgressAsync(string userId, string actionType, int actionValue = 1);
        Task<bool> UpdateLeaderboardsAsync();
    }
}
