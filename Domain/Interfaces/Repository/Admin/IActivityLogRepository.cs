using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IActivityLogRepository
    {
        Task<int> LogActivityAsync(ActivityLogCreateDto activity);
        Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10);
        Task<IEnumerable<RecentActivityDto>> GetEntityActivityAsync(string entityType, string entityId, int count = 10);
        Task<IEnumerable<RecentActivityDto>> GetUserActivityAsync(string userId, int count = 10);
    }
}
