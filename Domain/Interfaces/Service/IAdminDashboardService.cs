using Domain.Models.Admin;

namespace Domain.Interfaces.Service
{
    public interface IAdminDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10);
    }
}
