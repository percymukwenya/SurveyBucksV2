using Domain.Models.Admin;

namespace Domain.Interfaces.Service
{
    public interface ISurveyPreviewService
    {
        Task<SurveyPreviewDto> GetSurveyPreviewAsync(int surveyId, PreviewStateDto currentState = null);
        Task<PreviewNavigationDto> CalculateNavigationAsync(int surveyId, PreviewStateDto currentState);
        Task<List<string>> ValidateResponseAsync(int questionId, PreviewResponseDto response);
        Task<SurveyPreviewDto> ApplyLogicAsync(int surveyId, PreviewStateDto currentState);
        Task<bool> TestSurveyCompletionAsync(int surveyId, PreviewStateDto finalState);
    }
}
