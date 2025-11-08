namespace Domain.Models.Response
{
    public class SurveyParticipationSummaryDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public string UserId { get; set; }
        public DateTime EnrolmentDateTime { get; set; }
        public DateTime? StartedAtDateTime { get; set; }
        public DateTime? CompletedAtDateTime { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int ProgressPercentage { get; set; }
        public int? CurrentSectionId { get; set; }
        public int? CurrentQuestionId { get; set; }
        public int TimeSpentInSeconds { get; set; }
        public string CompletionCode { get; set; }
    }
}