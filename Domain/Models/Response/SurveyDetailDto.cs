namespace Domain.Models.Response
{
    public class SurveyDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime OpeningDateTime { get; set; }
        public DateTime ClosingDateTime { get; set; }
        public int DurationInSeconds { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string Industry { get; set; }
        public int MinQuestions { get; set; }
        public int MaxTimeInMins { get; set; }
        public bool RequireAllQuestions { get; set; }
        public List<RewardDto> Rewards { get; set; }
        public List<SurveySectionDto> Sections { get; set; }
    }
}
