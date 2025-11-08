namespace Domain.Models.Response
{
    public class SurveyFeedbackDto
    {
        public int SurveyParticipationId { get; set; }
        public int Rating { get; set; } // 1-5 star rating
        public string FeedbackText { get; set; }
        public string FeedbackCategory { get; set; } // Optional category
    }
}
