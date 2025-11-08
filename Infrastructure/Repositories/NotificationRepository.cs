using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public NotificationRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            string sql = @"
            SELECT n.Id, n.UserId, n.NotificationTypeId, nt.Name AS NotificationTypeName,
                   n.Title, n.Message, n.ReferenceId, n.ReferenceType, n.DeepLink,
                   n.IsRead, n.CreatedDate, n.ReadDate, n.ExpiryDate
            FROM SurveyBucks.UserNotification n
            JOIN SurveyBucks.NotificationType nt ON n.NotificationTypeId = nt.Id
            WHERE n.UserId = @UserId AND n.IsDeleted = 0";

            if (unreadOnly)
            {
                sql += " AND n.IsRead = 0";
            }

            sql += " ORDER BY n.CreatedDate DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // Track that notifications were viewed
                await connection.ExecuteAsync(
                    "UPDATE SurveyBucks.UserNotification SET IsSent = 1 WHERE UserId = @UserId AND IsSent = 0",
                    new { UserId = userId });

                return await connection.QueryAsync<NotificationDto>(sql, new { UserId = userId });
            }
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserNotification
            SET IsRead = 1,
                ReadDate = SYSDATETIMEOFFSET(),
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE Id = @NotificationId AND UserId = @UserId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { NotificationId = notificationId, UserId = userId });
                return result > 0;
            }
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserNotification
            SET IsRead = 1,
                ReadDate = SYSDATETIMEOFFSET(),
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE UserId = @UserId AND IsRead = 0 AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { UserId = userId });
                return result > 0;
            }
        }

        public async Task<bool> CreateNotificationAsync(string userId, string title, string message, string notificationType, string referenceId = null, string referenceType = null, string deepLink = null)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.UserNotification (
                UserId, NotificationTypeId, Title, Message,
                ReferenceId, ReferenceType, DeepLink,
                IsRead, IsSent, DeliveryChannel,
                CreatedDate, CreatedBy
            )
            SELECT 
                @UserId, nt.Id, @Title, @Message,
                @ReferenceId, @ReferenceType, @DeepLink,
                0, 0, 'InApp',
                SYSDATETIMEOFFSET(), 'system'
            FROM SurveyBucks.NotificationType nt
            WHERE nt.Name = @NotificationType";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    ReferenceId = referenceId,
                    ReferenceType = referenceType,
                    DeepLink = deepLink
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserNotification
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE Id = @NotificationId AND UserId = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { NotificationId = notificationId, UserId = userId });
                return result > 0;
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            const string sql = @"
            SELECT COUNT(1)
            FROM SurveyBucks.UserNotification
            WHERE UserId = @UserId AND IsRead = 0 AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            }
        }

        // Add this method to INotificationRepository interface
        public async Task<bool> SendSystemNotificationToAllUsersAsync(string title, string message, string notificationType)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.UserNotification (
                UserId, NotificationTypeId, Title, Message,
                IsRead, IsSent, DeliveryChannel,
                CreatedDate, CreatedBy
            )
            SELECT 
                u.Id, nt.Id, @Title, @Message,
                0, 0, 'InApp',
                SYSDATETIMEOFFSET(), 'system'
            FROM AspNetUsers u
            CROSS JOIN SurveyBucks.NotificationType nt
            WHERE nt.Name = @NotificationType";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    Title = title,
                    Message = message,
                    NotificationType = notificationType
                });

                return result > 0;
            }
        }

        public async Task<bool> SendNotificationToUserGroupAsync(IEnumerable<string> userIds, string title, string message, string notificationType)
        {
            if (userIds == null || !userIds.Any())
            {
                return false;
            }

            // Convert user IDs to a table parameter
            var userTable = new DataTable();
            userTable.Columns.Add("UserId", typeof(string));

            foreach (var userId in userIds)
            {
                userTable.Rows.Add(userId);
            }

            const string sql = @"
            INSERT INTO SurveyBucks.UserNotification (
                UserId, NotificationTypeId, Title, Message,
                IsRead, IsSent, DeliveryChannel,
                CreatedDate, CreatedBy
            )
            SELECT 
                u.UserId, nt.Id, @Title, @Message,
                0, 0, 'InApp',
                SYSDATETIMEOFFSET(), 'system'
            FROM @UserIds u
            CROSS JOIN SurveyBucks.NotificationType nt
            WHERE nt.Name = @NotificationType";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var parameter = new DynamicParameters();
                parameter.Add("@UserIds", userTable.AsTableValuedParameter("UserIdTableType"));
                parameter.Add("@Title", title);
                parameter.Add("@Message", message);
                parameter.Add("@NotificationType", notificationType);

                var result = await connection.ExecuteAsync(sql, parameter);
                return result > 0;
            }
        }
    }
}
