namespace Domain.Models.Response
{
    public class SurveyListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DurationInSeconds { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public string ThresholdStatus { get; set; } // Shows if standard or adjusted threshold was used
        public RewardSummaryDto Reward { get; set; }
        public int MatchScore { get; set; } // Optional, for ranked matching
        public List<string> MatchReasons { get; set; } = new();
    }
}
