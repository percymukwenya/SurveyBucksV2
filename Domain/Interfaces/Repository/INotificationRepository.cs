using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface INotificationRepository
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
        Task<bool> CreateNotificationAsync(string userId, string title, string message, string notificationType, string referenceId = null, string referenceType = null, string deepLink = null);
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<bool> SendSystemNotificationToAllUsersAsync(string title, string message, string notificationType);
        Task<bool> SendNotificationToUserGroupAsync(IEnumerable<string> userIds, string title, string message, string notificationType);
    }
}
