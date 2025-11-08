using Domain.Models.Response;

namespace Domain.Models
{
    public class SurveyAccessResponseDto
    {
        public bool HasAccess { get; set; }
        public int CompletionPercentage { get; set; }
        public string Message { get; set; }
        public List<string> BlockingFactors { get; set; } = new();
        public List<SurveyListItemDto> Surveys { get; set; } = new();
    }
}
