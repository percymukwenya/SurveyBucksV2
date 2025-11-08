using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Data;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Repositories
{
    public class UserEngagementRepository : IUserEngagementRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserEngagementRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserEngagementDto> GetUserEngagementAsync(string userId)
        {
            const string sql = @"
            SELECT ue.Id, ue.UserId, ue.LastLoginDate, ue.CurrentLoginStreak, 
                   ue.MaxLoginStreak, ue.TotalLogins, ue.TotalSurveysCompleted,
                   ue.TotalSurveysStarted, ue.CompletionRate, ue.TotalPointsEarned,
                   ue.TotalRewardsRedeemed, ue.LastActivityDate
            FROM SurveyBucks.UserEngagement ue
            WHERE ue.UserId = @UserId AND ue.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            var engagement = await connection.QuerySingleOrDefaultAsync<UserEngagementDto>(sql, new { UserId = userId });

            if (engagement == null)
            {
                // Create default engagement record
                engagement = await CreateDefaultEngagementAsync(connection, userId);
            }

            return engagement;
        }

        public async Task<bool> UpdateLoginStreakAsync(string userId, int newStreak, int maxStreak, DateTime loginDate)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserEngagement
            SET CurrentLoginStreak = @NewStreak,
                MaxLoginStreak = @MaxStreak,
                LastLoginDate = @LoginDate,
                TotalLogins = TotalLogins + 1,
                LastActivityDate = @LoginDate,
                ModifiedDate = SYSDATETIMEOFFSET()
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                NewStreak = newStreak,
                MaxStreak = maxStreak,
                LoginDate = loginDate
            });

            return result > 0;
        }

        public async Task<bool> UpdateSurveyStatsAsync(string userId, bool isCompletion = false)
        {
            var sql = isCompletion
                ? @"UPDATE SurveyBucks.UserEngagement
                SET TotalSurveysCompleted = TotalSurveysCompleted + 1,
                    LastActivityDate = SYSDATETIMEOFFSET(),
                    ModifiedDate = SYSDATETIMEOFFSET()
                WHERE UserId = @UserId"
                : @"UPDATE SurveyBucks.UserEngagement
                SET TotalSurveysStarted = TotalSurveysStarted + 1,
                    LastActivityDate = SYSDATETIMEOFFSET(),
                    ModifiedDate = SYSDATETIMEOFFSET()
                WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        public async Task<bool> UpdatePointsEarnedAsync(string userId, int pointsEarned)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserEngagement
            SET TotalPointsEarned = TotalPointsEarned + @PointsEarned,
                LastActivityDate = SYSDATETIMEOFFSET(),
                ModifiedDate = SYSDATETIMEOFFSET()
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, PointsEarned = pointsEarned });
            return result > 0;
        }

        public async Task<bool> UpdateRewardsRedeemedAsync(string userId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserEngagement
            SET TotalRewardsRedeemed = TotalRewardsRedeemed + 1,
                LastActivityDate = SYSDATETIMEOFFSET(),
                ModifiedDate = SYSDATETIMEOFFSET()
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId });
            return result > 0;
        }

        private async Task<UserEngagementDto> CreateDefaultEngagementAsync(IDbConnection connection, string userId)
        {
            const string insertSql = @"
            INSERT INTO SurveyBucks.UserEngagement (
                UserId, CurrentLoginStreak, MaxLoginStreak, TotalLogins,
                TotalSurveysCompleted, TotalSurveysStarted, TotalPointsEarned,
                TotalRewardsRedeemed, LastActivityDate, CreatedDate
            ) VALUES (
                @UserId, 0, 0, 0, 0, 0, 0, 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
            );
            
            SELECT SCOPE_IDENTITY() AS Id, @UserId AS UserId, NULL AS LastLoginDate, 
                   0 AS CurrentLoginStreak, 0 AS MaxLoginStreak, 0 AS TotalLogins,
                   0 AS TotalSurveysCompleted, 0 AS TotalSurveysStarted, 0 AS CompletionRate,
                   0 AS TotalPointsEarned, 0 AS TotalRewardsRedeemed, SYSDATETIMEOFFSET() AS LastActivityDate";

            return await connection.QuerySingleAsync<UserEngagementDto>(insertSql, new { UserId = userId });
        }
    }
}
