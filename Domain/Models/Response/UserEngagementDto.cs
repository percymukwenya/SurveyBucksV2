namespace Domain.Models.Response
{
    public class UserEngagementDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public int CurrentLoginStreak { get; set; }
        public int MaxLoginStreak { get; set; }
        public int TotalLogins { get; set; }
        public int TotalSurveysCompleted { get; set; }
        public int TotalSurveysStarted { get; set; }
        public decimal CompletionRate { get; set; }
        public int TotalPointsEarned { get; set; }
        public int TotalRewardsRedeemed { get; set; }
        public DateTimeOffset? LastActivityDate { get; set; }
    }
}
