using Application.Services.Auth;
using Domain.Interfaces.Repository.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Policy = "RequireAdminRole")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserManagementRepository _userManagementRepository;

        public UserController(IUserService userService, IUserManagementRepository userManagementRepository)
        {
            _userService = userService;
            _userManagementRepository = userManagementRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int take = 100, [FromQuery] int skip = 0)
        {
            try
            {
                var users = await _userManagementRepository.GetAllUsersAsync(take, skip);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            try
            {
                var userDetails = await _userManagementRepository.GetUserDetailsAsync(userId);
                if (userDetails == null)
                    return NotFound(new { message = "User not found" });

                return Ok(userDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user details", error = ex.Message });
            }
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var result = await _userService.DeleteUserByEmailAsync(email);

            if (result.Success)
            {
                return Ok(result.Message);
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(result.Message);
            }

            if (result.Message.Contains("required"))
            {
                return BadRequest(result.Message);
            }

            return StatusCode(500, result.Message);
        }
    }
}
