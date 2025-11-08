using Domain.Models.Response;

namespace Domain.Models
{
    public class UserDashboardDto
    {
        public UserPointsDto UserPoints { get; set; }
        public UserLevelDto UserLevel { get; set; }
        public UserEngagementDto Engagement { get; set; }
        public decimal ProfileCompletionPercentage { get; set; }
        public List<SurveyListItemDto> AvailableSurveys { get; set; }
        public List<SurveyParticipationSummaryDto> InProgressSurveys { get; set; }
        public List<SurveyParticipationSummaryDto> CompletedSurveys { get; set; }
        public List<UserRewardDto> RecentRewards { get; set; }
        public List<ChallengeDto> ActiveChallenges { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; }
    }
}
