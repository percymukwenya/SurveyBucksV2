using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface IGamificationService
    {
        // Rich business operations
        Task<bool> UpdateLoginStreakAsync(string userId);
        Task ProcessEnrollmentAsync(string userId, int surveyId);
        Task ProcessSurveyCompletionAsync(string userId, int surveyId);
        Task ProcessQuestionAnsweredAsync(string userId, int questionId);
        Task ProcessProgressUpdateAsync(string userId, int progressPercentage);
        Task ProcessRewardRedemptionAsync(string userId, int rewardId, int pointsUsed);
        Task ProcessRewardClaimAsync(string userId);

        // Simple delegation operations
        Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId);
        Task<IEnumerable<LeaderboardSummaryDto>> GetAvailableLeaderboardsAsync(string userId);
        Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10);
        Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(string userId);
        Task<UserLevelDto> GetUserLevelAsync(string userId);
        Task<UserStatsDto> GetUserStatsAsync(string userId);

        // Profile-specific gamification
        Task ProcessProfileMilestoneAsync(string userId, string achievementKey, int completionPercentage);
        Task ProcessChallengeProgressAsync(string userId, string actionType, int actionValue);

        Task ProcessPointsEarnedAsync(string userId, int points, string actionType, string referenceId = null);
        Task ProcessDocumentUploadAsync(string userId, string documentType);
        Task ProcessDocumentVerificationAsync(string userId, string documentTypeName);
    }
}
