using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Dapper;

namespace Infrastructure.Repositories.Admin
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public ActivityLogRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<RecentActivityDto>> GetEntityActivityAsync(string entityType, string entityId, int count = 10)
        {
            const string sql = @"
            SELECT TOP(@Count)
                la.Id,
                la.ActivityType,
                la.Title,
                la.Description,
                la.ActivityDateTime AS Timestamp,
                la.ActionUrl,
                la.EntityType,
                la.EntityId,
                u.UserName AS InitiatedBy
            FROM SurveyBucks.LogActivity la
            LEFT JOIN SurveyBucks.Users u ON la.UserId = u.Id
            WHERE la.EntityType = @EntityType
                AND (@EntityId IS NULL OR la.EntityId = @EntityId)
            ORDER BY la.ActivityDateTime DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<RecentActivityDto>(sql, new { Count = count, EntityType = entityType, EntityId = entityId });
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10)
        {
            const string sql = @"
            SELECT TOP(@Count)
                la.Id,
                la.ActivityType,
                la.Title,
                la.Description,
                la.ActivityDateTime AS Timestamp,
                la.ActionUrl,
                la.EntityType,
                la.EntityId,
                u.UserName AS InitiatedBy
            FROM SurveyBucks.LogActivity la
            LEFT JOIN SurveyBucks.Users u ON la.UserId = u.Id
            ORDER BY la.ActivityDateTime DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<RecentActivityDto>(sql, new { Count = count });
        }

        public async Task<IEnumerable<RecentActivityDto>> GetUserActivityAsync(string userId, int count = 10)
        {
            const string sql = @"
            SELECT TOP(@Count)
                la.Id,
                la.ActivityType,
                la.Title,
                la.Description,
                la.ActivityDateTime AS Timestamp,
                la.ActionUrl,
                la.EntityType,
                la.EntityId,
                u.UserName AS InitiatedBy
            FROM SurveyBucks.LogActivity la
            LEFT JOIN AspNetUsers u ON la.UserId = u.Id
            WHERE la.UserId = @UserId
            ORDER BY la.ActivityDateTime DESC";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<RecentActivityDto>(sql, new { Count = count, UserId = userId });
        }

        public async Task<int> LogActivityAsync(ActivityLogCreateDto activity)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.LogActivity (
                ActivityType, Title, Description, ActivityDateTime,
                ActionUrl, EntityType, EntityId, UserId,
                CreatedDate, CreatedBy
            ) VALUES (
                @ActivityType, @Title, @Description, SYSDATETIMEOFFSET(),
                @ActionUrl, @EntityType, @EntityId, @UserId,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<int>(sql, activity);
        }
    }
}
