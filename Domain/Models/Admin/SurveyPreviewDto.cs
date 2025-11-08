using Domain.Models.Response;

namespace Domain.Models.Admin
{
    public class SurveyPreviewDto
    {
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public List<SectionPreviewDto> Sections { get; set; }
        public Dictionary<string, object> TestData { get; set; } // For testing logic
    }

    public class SectionPreviewDto
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public List<QuestionPreviewDto> Questions { get; set; }
    }

    public class QuestionPreviewDto
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string QuestionType { get; set; }
        public bool IsMandatory { get; set; }
        public string HelpText { get; set; }
        public List<QuestionResponseChoiceDto> Choices { get; set; }
        public List<QuestionLogicDto> Logic { get; set; }
        public bool IsVisible { get; set; } = true;
        public string VisibilityReason { get; set; }
    }

    public class PreviewNavigationDto
    {
        public int CurrentSectionIndex { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public string NextAction { get; set; } // 'NextQuestion', 'NextSection', 'Complete'
        public int? NextQuestionId { get; set; }
        public int? NextSectionId { get; set; }
    }

    public class PreviewResponseDto
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; }
        public Dictionary<int, string> MatrixAnswers { get; set; } // For matrix questions
    }

    public class PreviewStateDto
    {
        public int SurveyId { get; set; }
        public Dictionary<int, PreviewResponseDto> Responses { get; set; }
        public List<int> VisitedQuestions { get; set; }
        public List<int> SkippedQuestions { get; set; }
        public PreviewNavigationDto Navigation { get; set; }
        public List<string> ValidationErrors { get; set; }
    }
}
