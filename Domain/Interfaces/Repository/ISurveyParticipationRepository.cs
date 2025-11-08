using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface ISurveyParticipationRepository
    {
        Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysForUserAsync(string userId, int matchThreshold = 70);
        Task<int> CreateParticipationAsync(SurveyParticipationDto participation);
        Task<int?> GetExistingParticipationIdAsync(string userId, int surveyId);
        Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId);
        Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId, string userId);
        Task<int> EnrollInSurveyAsync(string userId, int surveyId);
        Task<SurveyParticipationDto> GetParticipationAsync(int participationId, string userId);
        Task<bool> UpdateParticipationProgressAsync(int participationId, string userId, int sectionId, int questionId, int progressPercentage);
        Task<bool> CompleteSurveyAsync(int participationId, string userId);
        Task<IEnumerable<SurveyParticipationSummaryDto>> GetUserParticipationsAsync(string userId, string status = null);
        Task<bool> SaveSurveyResponseAsync(SurveyResponseDto response);
        Task<IEnumerable<SurveyResponseDto>> GetSavedResponsesAsync(int participationId, string userId);

        Task<SurveySectionDetailDto> GetSectionWithQuestionsAsync(int sectionId, string userId);
        Task<SurveyProgressDto> GetSurveyProgressAsync(int surveyId, string userId);
        Task<NavigationDto> GetSurveyNavigationAsync(int surveyId, string userId);
        Task<DetailedProfileCompletionDto> GetDetailedProfileCompletionAsync(string userId);
    }
}
