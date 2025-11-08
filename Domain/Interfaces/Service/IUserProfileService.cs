using Domain.Models;
using Domain.Models.Request;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface IUserProfileService
    {
        Task<DemographicsDto> GetUserDemographicsAsync(string userId);
        Task<bool> UpdateDemographicsAsync(UpdateDemographicsRequest demographics, string userId);
        Task<decimal> GetProfileCompletionPercentageAsync(string userId);
        Task<IEnumerable<UserInterestDto>> GetUserInterestsAsync(string userId);
        Task<bool> AddUserInterestAsync(string userId, string interest, int? interestLevel = null);
        Task<bool> RemoveUserInterestAsync(string userId, string interest);
        Task<UserEngagementDto> GetUserEngagementAsync(string userId);
        Task<UserDashboardDto> GetUserDashboardAsync(string userId);
    }
}
