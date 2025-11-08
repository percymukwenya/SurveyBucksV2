using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IAdminDashboardRepository _adminDashboardRepository;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(IActivityLogRepository activityLogRepository, 
            IAdminDashboardRepository adminDashboardRepository, ILogger<AdminDashboardService> logger)
        {
            _activityLogRepository = activityLogRepository;
            _adminDashboardRepository = adminDashboardRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            return await _adminDashboardRepository.GetDashboardStatsAsync();
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10)
        {
            return await _activityLogRepository.GetRecentActivityAsync(count);
        }
    }
}
