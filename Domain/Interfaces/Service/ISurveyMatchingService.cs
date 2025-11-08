using Domain.Models;

namespace Domain.Interfaces.Service
{
    public interface ISurveyMatchingService
    {
        Task<IEnumerable<SurveyMatchDto>> GetMatchingSurveysAsync(string userId, int matchThreshold = 70);
    }
}
