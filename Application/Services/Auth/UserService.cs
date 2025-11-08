using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System;
using Application.Models;
using Domain.Models;

namespace Application.Services.Auth
{
    public interface IUserService
    {
        Task<ServiceResult> DeleteUserByEmailAsync(string email);
    } 

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult> DeleteUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ServiceResult.FailureResult("Email is required");
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found", email);
                    return ServiceResult.FailureResult($"User with email '{email}' not found");
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User with email {Email} deleted successfully", email);
                    return ServiceResult.SuccessResult($"User with email '{email}' deleted successfully");
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to delete user {Email}: {Errors}", email, string.Join(", ", errors));
                return ServiceResult.FailureResult("Failed to delete user", errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with email {Email}", email);
                return ServiceResult.FailureResult("An error occurred while deleting the user");
            }
        }
    }
}
