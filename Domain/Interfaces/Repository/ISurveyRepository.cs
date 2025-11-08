using Domain.Models;

namespace Domain.Interfaces.Repository
{
    public interface ISurveyRepository
    {
        Task<IEnumerable<SurveyDto>> GetActiveSurveysAsync();
        Task<SurveyDto> GetByIdAsync(int surveyId);
        Task<bool> HasUserParticipatedAsync(int surveyId, string userId);
    }
}
