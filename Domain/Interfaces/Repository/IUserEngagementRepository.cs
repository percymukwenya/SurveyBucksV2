using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IUserEngagementRepository
    {
        Task<UserEngagementDto> GetUserEngagementAsync(string userId);
        Task<bool> UpdateLoginStreakAsync(string userId, int newStreak, int maxStreak, DateTime loginDate);
        Task<bool> UpdateSurveyStatsAsync(string userId, bool isCompletion = false);
        Task<bool> UpdatePointsEarnedAsync(string userId, int pointsEarned);
        Task<bool> UpdateRewardsRedeemedAsync(string userId);        
    }
}
