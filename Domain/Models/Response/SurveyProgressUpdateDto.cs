namespace Domain.Models.Response
{
    public class SurveyProgressUpdateDto
    {
        public int ParticipationId { get; set; }
        public int SectionId { get; set; }
        public int QuestionId { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
