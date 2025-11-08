namespace Domain.Models.Admin
{
    public class DashboardStatsDto
    {
        // User statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public double AverageProfileCompletion { get; set; }
        public int PendingVerifications { get; set; }

        // Survey statistics
        public int TotalSurveys { get; set; }
        public int ActiveSurveys { get; set; }
        public int DraftSurveys { get; set; }
        public int PublishedSurveys { get; set; }
        public int CompletedSurveys { get; set; }
        public int SurveyCompletions { get; set; }
        public int SurveysWithNewResults { get; set; }

        // Analytics statistics
        public int ExportsPending { get; set; }
        public double AverageCompletionRate { get; set; }
        public string AverageResponseTime { get; set; }

        // Reward statistics
        public int RewardsRedeemed { get; set; }
        public int ActiveRewards { get; set; }
        public int PendingRedemptions { get; set; }
        public int PointsIssued { get; set; }
        public int PointsRedeemed { get; set; }
    }
}
