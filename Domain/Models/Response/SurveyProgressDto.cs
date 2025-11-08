using Domain.Models.Admin;

namespace Domain.Models.Response
{
    public class SurveyProgressDto
    {
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
        public int ParticipationId { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int ProgressPercentage { get; set; }
        public int? CurrentSectionId { get; set; }
        public int? CurrentQuestionId { get; set; }
        public int TimeSpentInSeconds { get; set; }
        public int? MaxTimeInSeconds { get; set; }
        public int TotalSections { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
    }

    public class NavigationDto
    {
        public int SurveyId { get; set; }
        public List<SectionNavigationDto> Sections { get; set; } = new();
    }

    public class SectionNavigationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public int QuestionCount { get; set; }
        public int AnsweredCount { get; set; }
        public int CompletionPercentage { get; set; }
    }

    public class SurveySectionDetailDto
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool RequireAllQuestions { get; set; }
        public int? MaxTimeInMins { get; set; }
        public List<ParticipationQuestionDetailDto> Questions { get; set; } = new();
    }

    public class ParticipationQuestionDetailDto
    {
        public int Id { get; set; }
        public int SurveySectionId { get; set; }
        public string Text { get; set; }
        public bool IsMandatory { get; set; }
        public int Order { get; set; }
        public int QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public bool HasChoices { get; set; }
        public bool HasMatrix { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ValidationMessage { get; set; }
        public string HelpText { get; set; }
        public bool IsScreeningQuestion { get; set; }
        public int? TimeoutInSeconds { get; set; }
        public bool RandomizeChoices { get; set; }

        public List<QuestionResponseChoiceDto> ResponseChoices { get; set; } = new();
        public List<MatrixRowDto> MatrixRows { get; set; } = new();
        public List<MatrixColumnDto> MatrixColumns { get; set; } = new();
        public List<SurveyResponseDto> SavedResponses { get; set; } = new();
    }

    public class SurveyPauseDto
    {
        public string Reason { get; set; }
        public int CurrentQuestionId { get; set; }
        public Dictionary<string, object> SessionData { get; set; } = new();
    }

    public class SurveyResumeDto
    {
        public int CurrentSectionId { get; set; }
        public int CurrentQuestionId { get; set; }
        public int ProgressPercentage { get; set; }
        public Dictionary<string, object> SessionData { get; set; } = new();
        public TimeSpan PauseDuration { get; set; }
    }

    public class QuestionTimeTrackingDto
    {
        public int QuestionId { get; set; }
        public DateTimeOffset ViewStartTime { get; set; }
        public DateTimeOffset? ViewEndTime { get; set; }
        public bool IsAnswered { get; set; }
        public string DeviceInfo { get; set; }
    }

    public class SurveyValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<QuestionValidationDto> QuestionValidations { get; set; } = new();
    }

    public class CompletionValidationResult
    {
        public bool CanComplete { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class SurveyCompletionSummaryDto
    {
        public int ParticipationId { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public int TimeSpentInSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public int PointsEarned { get; set; }
        public List<RewardDto> RewardsEarned { get; set; } = new();
        public string CompletionCode { get; set; }
    }

    public class ResponseValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int? ResponseId { get; set; }
        public bool IsScreeningResponse { get; set; }
        public ScreeningResult ScreeningResult { get; set; }
        public ConditionalAction NextAction { get; set; }

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
    }

    public class BatchResponseResult
    {
        public List<SurveyResponseDto> ValidResponses { get; set; } = new();
        public List<FailedResponse> FailedResponses { get; set; } = new();
        public int SuccessCount { get; set; }
    }

    public class FailedResponse
    {
        public int QuestionId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class QuestionValidationDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsMandatory { get; set; }
        public int QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string ValidationMessage { get; set; }
        public string HelpText { get; set; }
        public bool IsScreeningQuestion { get; set; }
        public string ScreeningLogic { get; set; }
        public bool RandomizeChoices { get; set; }
        public bool HasChoices { get; set; }
        public bool HasMinMaxValues { get; set; }
        public bool HasFreeText { get; set; }
        public bool HasMatrix { get; set; }
        public string ValidationRegex { get; set; }
        public int? DefaultMinValue { get; set; }
        public int? DefaultMaxValue { get; set; }
    }

    public class ScreeningResult
    {
        public bool IsQualified { get; set; }
        public string DisqualificationReason { get; set; }
    }

    public class ConditionalAction
    {
        public string ActionType { get; set; } // "skip_to", "end_survey", "show_section"
        public int? TargetQuestionId { get; set; }
        public int? TargetSectionId { get; set; }
    }

    public class ConditionalLogicDto
    {
        public string ConditionType { get; set; }
        public string ConditionValue { get; set; }
        public string ActionType { get; set; }
        public int? TargetQuestionId { get; set; }
        public int? TargetSectionId { get; set; }
    }
}
