using Domain.Models;

namespace Domain.Interfaces.Service
{
    public interface IUserProfileCompletionService
    {
        Task<UserProfileCompletionDto> GetProfileCompletionAsync(string userId);
        Task<ProfileUpdateResultDto> ProcessProfileUpdateAsync(string userId, string sectionUpdated);

        Task<bool> IsEligibleForSurveysAsync(string userId);
        Task<List<string>> GetProfileCompletionSuggestionsAsync(string userId);
    }
}
