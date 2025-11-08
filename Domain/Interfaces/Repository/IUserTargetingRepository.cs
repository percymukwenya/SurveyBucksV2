using Domain.Models;

namespace Domain.Interfaces.Repository
{
    public interface IUserTargetingRepository
    {
        Task<SurveyTargetingDto> GetSurveyTargetingAsync(int surveyId);
    }
}
