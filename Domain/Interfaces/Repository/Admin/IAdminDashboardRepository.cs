using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IAdminDashboardRepository
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
