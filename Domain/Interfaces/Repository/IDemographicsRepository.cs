using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IDemographicsRepository
    {
        Task<DemographicsDto> GetUserDemographicsAsync(string userId);
        Task<UserProfileDto> GetUserProfileForMatchingAsync(string userId);
        Task<bool> UpdateDemographicsAsync(DemographicsDto demographics, string updatedBy);
        Task<decimal> GetProfileCompletionPercentageAsync(string userId);
        Task<UserProfileCompletionDto> GetSimplifiedProfileCompletionAsync(string userId);
        Task<IEnumerable<UserInterestDto>> GetUserInterestsAsync(string userId);
        Task<bool> AddUserInterestAsync(string userId, string interest, int? interestLevel);
        Task<bool> RemoveUserInterestAsync(string userId, string interest);

        Task<DemographicMatchSummaryDto> GetDemographicMatchSummaryAsync(string userId);
        Task<List<string>> GetSuggestedFieldsToCompleteAsync(string userId);
        Task<int> GetPotentialMatchingCountAsync(string userId);
        Task<bool> HasSufficientDemographicDataAsync(string userId);
        Task<List<string>> GetUserInterestNamesAsync(string userId);
    }
}
