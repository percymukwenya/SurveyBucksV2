using Domain.Models.Admin;

namespace Domain.Interfaces.Repository.Admin
{
    public interface IUserManagementRepository
    {
        Task<IEnumerable<UserAdminDto>> GetAllUsersAsync(int take = 100, int skip = 0);
        Task<UserDetailAdminDto> GetUserDetailsAsync(string userId);
        Task<IEnumerable<AuditLogDto>> GetUserAuditLogAsync(string userId);
        Task<bool> UpdateUserPointsAsync(string userId, int pointsToAdd, string reason, string modifiedBy);
        Task<bool> BanUserAsync(string userId, string reason, string bannedBy);
        Task<bool> UnbanUserAsync(string userId, string unbannedBy);
    }
}
