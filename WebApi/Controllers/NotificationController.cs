using Domain.Interfaces.Service;
using Domain.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetUnreadNotificationCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
            return Ok(count);
        }

        [HttpPost("{notificationId}/read")]
        public async Task<ActionResult> MarkNotificationAsRead(int notificationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);
            if (result)
                return Ok();
            return NotFound();
        }

        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllNotificationsAsRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok();
        }
    }
}
