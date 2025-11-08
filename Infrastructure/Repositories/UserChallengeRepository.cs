using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserChallengeRepository : IUserChallengeRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserChallengeRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId)
        {
            const string sql = @"
            SELECT c.Id, c.Name, c.Description, c.StartDate, c.EndDate,
                   c.RequiredActionType, c.RequiredActionCount, c.PointsAwarded,
                   c.RewardId, c.ImageUrl, c.IsActive,
                   uc.Progress, uc.IsCompleted, uc.CompletedDate
            FROM SurveyBucks.Challenges c
            LEFT JOIN SurveyBucks.UserChallenges uc ON c.Id = uc.ChallengeId AND uc.UserId = @UserId AND uc.IsDeleted = 0
            WHERE c.IsActive = 1 
              AND c.IsDeleted = 0
              AND GETDATE() BETWEEN c.StartDate AND c.EndDate
            ORDER BY c.EndDate ASC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ChallengeDto>(sql, new { UserId = userId });
        }

        public async Task<UserChallengeDto> GetUserChallengeAsync(string userId, int challengeId)
        {
            const string sql = @"
            SELECT Id, UserId, ChallengeId, Progress, IsCompleted, CompletedDate, PointsAwarded, IsRewarded
            FROM SurveyBucks.UserChallenges
            WHERE UserId = @UserId AND ChallengeId = @ChallengeId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserChallengeDto>(sql, new { UserId = userId, ChallengeId = challengeId });
        }

        public async Task<bool> UpdateChallengeProgressAsync(string userId, int challengeId, int progress, bool isCompleted, int? pointsAwarded = null)
        {
            const string sql = @"
            MERGE SurveyBucks.UserChallenges AS target
            USING (SELECT @UserId AS UserId, @ChallengeId AS ChallengeId) AS source
            ON target.UserId = source.UserId AND target.ChallengeId = source.ChallengeId AND target.IsDeleted = 0
            WHEN MATCHED THEN
                UPDATE SET 
                    Progress = @Progress,
                    IsCompleted = @IsCompleted,
                    CompletedDate = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 THEN SYSDATETIMEOFFSET() ELSE CompletedDate END,
                    PointsAwarded = CASE WHEN @PointsAwarded IS NOT NULL THEN @PointsAwarded ELSE PointsAwarded END,
                    IsRewarded = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 THEN 1 ELSE IsRewarded END,
                    ModifiedDate = SYSDATETIMEOFFSET()
            WHEN NOT MATCHED THEN
                INSERT (UserId, ChallengeId, Progress, IsCompleted, CompletedDate, PointsAwarded, IsRewarded, CreatedDate)
                VALUES (@UserId, @ChallengeId, @Progress, @IsCompleted, 
                       CASE WHEN @IsCompleted = 1 THEN SYSDATETIMEOFFSET() ELSE NULL END,
                       @PointsAwarded, CASE WHEN @IsCompleted = 1 THEN 1 ELSE 0 END, SYSDATETIMEOFFSET());";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                ChallengeId = challengeId,
                Progress = progress,
                IsCompleted = isCompleted,
                PointsAwarded = pointsAwarded
            });

            return result > 0;
        }

        public async Task<ChallengeDto> GetChallengeByIdAsync(int challengeId)
        {
            const string sql = @"
            SELECT Id, Name, Description, StartDate, EndDate, RequiredActionType, 
                   RequiredActionCount, PointsAwarded, RewardId, ImageUrl, IsActive
            FROM SurveyBucks.Challenges
            WHERE Id = @ChallengeId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<ChallengeDto>(sql, new { ChallengeId = challengeId });
        }
    }
}
