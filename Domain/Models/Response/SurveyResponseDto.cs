namespace Domain.Models.Response
{
    public class SurveyResponseDto
    {
        public int Id { get; set; }
        public int SurveyParticipationId { get; set; }
        public int QuestionId { get; set; }
        public string Answer { get; set; }
        public DateTime ResponseDateTime { get; set; }
        public int? MatrixRowId { get; set; }
    }
}
