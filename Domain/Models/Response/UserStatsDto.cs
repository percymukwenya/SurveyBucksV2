namespace Domain.Models.Response
{
    public class UserStatsDto
    {
        public int TotalSurveys { get; set; }
        public int TotalPoints { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int AchievementsUnlocked { get; set; }
        public int ChallengesCompleted { get; set; }
    }
}