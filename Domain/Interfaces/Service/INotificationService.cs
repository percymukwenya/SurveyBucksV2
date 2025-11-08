using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<bool> CreateNotificationAsync(string userId, string title, string message, string notificationType, string referenceId = null, string referenceType = null, string deepLink = null);
        Task<bool> SendSystemNotificationToAllUsersAsync(string title, string message, string notificationType);
        Task<bool> SendNotificationToUserGroupAsync(IEnumerable<string> userIds, string title, string message, string notificationType);

        Task SendEnrollmentNotificationAsync(string userId, int surveyId);
        Task SendCompletionNotificationAsync(string userId, int surveyId);
        Task SendAchievementNotificationAsync(string userId, string achievementKey);
        Task SendLevelUpNotificationAsync(string userId, int newLevel);

        Task SendRewardRedeemedNotificationAsync(string userId, string rewardName, string redemptionCode);
        Task SendRewardClaimedNotificationAsync(string userId, string rewardName);
        Task SendRewardDeliveryNotificationAsync(string userId, string rewardName, string trackingInfo = null);
        Task SendLowPointsWarningAsync(string userId, string rewardName, int pointsNeeded, int pointsAvailable);

        Task SendFirstRedemptionBonusNotificationAsync(string userId);
        Task SendChallengeCompletedNotificationAsync(string userId, string challengeName, int pointsAwarded);
        Task SendStreakNotificationAsync(string userId, int streakDays);
        Task SendMilestoneNotificationAsync(string userId, string milestoneType, int milestoneValue, int bonusPoints);
        Task SendEngagementEncouragementAsync(string userId, string encouragementType);
        Task SendLeaderboardUpdateAsync(string userId, string leaderboardName, int currentRank, int? previousRank);

        // Profile completion notifications
        Task SendProfileMilestoneNotificationAsync(string userId, string milestone, int completionPercentage);
        Task SendProfileCompletionNotificationAsync(string userId);
        Task SendProfileNearCompletionNotificationAsync(string userId, int percentageRemaining);
        Task SendProfileReminderNotificationAsync(string userId, string sectionName);

        Task SendDocumentUploadedNotificationAsync(string userId, string documentTypeName);

        Task SendDocumentApprovedNotificationAsync(string userId, string documentTypeName);
        Task SendDocumentRejectedNotificationAsync(string userId, string documentTypeName, string reason);
        Task SendDocumentDeletedNotificationAsync(string userId, string documentTypeName);

        Task SendSectionCompletionNotificationAsync(string userId, string sectionName, int sectionPercentage);
        Task SendProfileNextStepSuggestionAsync(string userId, string nextSection, int timeEstimate);
        Task SendWeeklyProfileGoalAsync(string userId, int currentCompletion, string focusSection);
        Task SendSurveyEligibilityUnlockedNotificationAsync(string userId);
        Task SendProfileBoostNotificationAsync(string userId, string actionType, int pointsEarned);
        Task SendProfileSectionEncouragementAsync(string userId, string sectionName, int currentProgress);
        Task SendDetailedMilestoneNotificationAsync(string userId, string milestone, int percentage, int sectionsComplete);
    }
}
