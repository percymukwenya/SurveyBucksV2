namespace Domain.Models.Response
{
    public class SurveySectionDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public List<QuestionDto> Questions { get; set; }
    }
}
