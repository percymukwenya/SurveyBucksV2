namespace Domain.Models.Admin
{
    public class SurveyResponseExportDto
    {
        public string SurveyName { get; set; }
        public int ParticipationId { get; set; }
        public string Participant { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Answer { get; set; }
        public DateTime ResponseDateTime { get; set; }
    }
}
