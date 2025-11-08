using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface ISurveyParticipationService
    {
        Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysForUserAsync(string userId);
        Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId, string userId);
        Task<SurveyParticipationDto> EnrollInSurveyAsync(string userId, int surveyId);
        Task<SurveyParticipationDto> GetParticipationAsync(int participationId, string userId);
        Task<bool> UpdateParticipationProgressAsync(SurveyProgressUpdateDto progressDto, string userId);
        Task<bool> CompleteSurveyAsync(int participationId, string userId);
        Task<IEnumerable<SurveyParticipationSummaryDto>> GetUserParticipationsAsync(string userId, string status = null);
        Task<bool> SaveSurveyResponseAsync(SurveyResponseDto response, string userId);
        Task<IEnumerable<SurveyResponseDto>> GetSavedResponsesAsync(int participationId, string userId);
        Task<bool> SubmitSurveyFeedbackAsync(SurveyFeedbackDto feedback, string userId);
        Task<SurveyAccessResponseDto> GetAvailableSurveysWithAccessCheckAsync(string userId);
    }
}
