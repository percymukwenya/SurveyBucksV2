using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Domain.Models;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace Infrastructure.Repositories
{
    public class UserAchievementRepository : IUserAchievementRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserAchievementRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(string userId)
        {
            const string sql = @"
            SELECT ua.Id, ua.UserId, ua.AchievementId, a.Name AS AchievementName,
                   a.Description AS AchievementDescription, a.Category, a.ImageUrl,
                   ua.PointsAwarded, ua.EarnedDate, ua.EarnedCount, ua.LastEarnedDate
            FROM SurveyBucks.UserAchievements ua
            JOIN SurveyBucks.Achievements a ON ua.AchievementId = a.Id
            WHERE ua.UserId = @UserId AND ua.IsDeleted = 0
            ORDER BY ua.LastEarnedDate DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserAchievementDto>(sql, new { UserId = userId });
        }

        public async Task<bool> HasAchievementAsync(string userId, string achievementKey)
        {
            const string sql = @"
            SELECT COUNT(1) 
            FROM SurveyBucks.UserAchievements ua
            JOIN SurveyBucks.Achievements a ON ua.AchievementId = a.Id
            WHERE ua.UserId = @UserId AND a.Name = @AchievementKey AND ua.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, AchievementKey = achievementKey });
            return count > 0;
        }

        public async Task<bool> UnlockAchievementAsync(string userId, string achievementKey, int pointsAwarded)
        {
            const string sql = @"
            DECLARE @AchievementId INT;
            SELECT @AchievementId = Id FROM SurveyBucks.Achievements WHERE Name = @AchievementKey;
            
            IF @AchievementId IS NOT NULL
            BEGIN
                MERGE SurveyBucks.UserAchievements AS target
                USING (SELECT @UserId AS UserId, @AchievementId AS AchievementId) AS source
                ON target.UserId = source.UserId AND target.AchievementId = source.AchievementId AND target.IsDeleted = 0
                WHEN MATCHED THEN
                    UPDATE SET 
                        EarnedCount = EarnedCount + 1,
                        LastEarnedDate = SYSDATETIMEOFFSET(),
                        PointsAwarded = PointsAwarded + @PointsAwarded,
                        IsNotified = 0,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = 'system'
                WHEN NOT MATCHED THEN
                    INSERT (UserId, AchievementId, EarnedDate, PointsAwarded, EarnedCount, LastEarnedDate, IsNotified, CreatedDate, CreatedBy)
                    VALUES (@UserId, @AchievementId, SYSDATETIMEOFFSET(), @PointsAwarded, 1, SYSDATETIMEOFFSET(), 0, SYSDATETIMEOFFSET(), 'system');
                
                SELECT 1;
            END
            ELSE
            BEGIN
                SELECT 0;
            END";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                AchievementKey = achievementKey,
                PointsAwarded = pointsAwarded
            });

            return result > 0;
        }

        public async Task<AchievementDto> GetAchievementByKeyAsync(string achievementKey)
        {
            const string sql = @"
            SELECT Id, Name, Description, Category, ImageUrl, PointsAwarded, 
                   RequiredActionType, RequiredActionCount, IsRepeatable, RepeatCooldownDays
            FROM SurveyBucks.Achievements
            WHERE Name = @AchievementKey AND IsActive = 1 AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<AchievementDto>(sql, new { AchievementKey = achievementKey });
        }
    }
}
