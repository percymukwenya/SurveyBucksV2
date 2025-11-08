using Domain.Models.Response;

namespace Domain.Models
{
    public class SurveyAccessResultDto
    {
        public bool HasAccess { get; set; }
        public int CompletionPercentage { get; set; }
        public string Message { get; set; }
        public List<string> BlockingFactors { get; set; } = new();
        public List<IncompleteSectionDto> IncompleteSections { get; set; } = new();
        public List<SurveyListItemDto> AvailableSurveys { get; set; } = new();
        public int TotalAvailableSurveys { get; set; }
    }

    public class IncompleteSectionDto
    {
        public string SectionName { get; set; }
        public int CompletionPercentage { get; set; }
        public List<string> MissingItems { get; set; }
        public int EstimatedTimeMinutes { get; set; }
        public int Priority { get; set; }
        public string CallToAction => $"Complete {SectionName} ({EstimatedTimeMinutes} min)";
    }
}
