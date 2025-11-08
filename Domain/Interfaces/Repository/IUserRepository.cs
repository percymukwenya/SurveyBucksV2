using Domain.Models;

namespace Domain.Interfaces.Repository
{
    public interface IUserRepository
    {
        Task<UserDto> GetUserByIdAsync(string userId);
        Task<string> GetUserIdByEmailAsync(string email);
        Task<bool> DeleteUserWithRolesAsync(string userId);
    }
}
