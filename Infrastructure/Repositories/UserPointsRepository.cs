using Dapper;
using Domain.Interfaces.Repository;
using Infrastructure.Shared;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserPointsRepository : IUserPointsRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserPointsRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AddPointsAsync(string userId, int points, string actionType, string referenceId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update user points
                const string updatePointsSql = @"
                UPDATE SurveyBucks.UserPoints
                SET TotalPoints = TotalPoints + @Points,
                    AvailablePoints = AvailablePoints + @Points,
                    LastPointEarnedDate = SYSDATETIMEOFFSET(),
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @UserId
                WHERE UserId = @UserId";

                await connection.ExecuteAsync(updatePointsSql, new { UserId = userId, Points = points }, transaction);

                // Create transaction record
                const string transactionSql = @"
                INSERT INTO SurveyBucks.PointTransactions (
                    UserId, Amount, TransactionType, ActionType, Description,
                    ReferenceId, ReferenceType, TransactionDate, CreatedDate, CreatedBy
                ) VALUES (
                    @UserId, @Amount, 'Earned', @ActionType, @Description,
                    @ReferenceId, 'Action', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 'system'
                )";

                await connection.ExecuteAsync(transactionSql, new
                {
                    UserId = userId,
                    Amount = points,
                    ActionType = actionType,
                    Description = $"Points earned for {actionType}",
                    ReferenceId = referenceId
                }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public async Task<int> GetTotalPointsAsync(string userId)
        {
            const string sql = @"
            SELECT ISNULL(TotalPoints, 0)
            FROM SurveyBucks.UserPoints
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<int> GetActionCountAsync(string userId, string actionType)
        {
            const string sql = @"
            SELECT COUNT(*)
            FROM SurveyBucks.PointTransactions
            WHERE UserId = @UserId AND ActionType = @ActionType AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, ActionType = actionType });
        }

        public async Task<int> GetUserLevelAsync(string userId)
        {
            const string sql = @"
            SELECT ISNULL(PointsLevel, 1)
            FROM SurveyBucks.UserPoints
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<bool> UpdateUserLevelAsync(string userId, int level)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserPoints
            SET PointsLevel = @Level,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = 'system'
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserId = userId, Level = level });
            return result > 0;
        }

        public async Task<int> GetTotalPointsSpentAsync(string userId)
        {
            const string sql = @"
            SELECT ISNULL(RedeemedPoints, 0)
            FROM SurveyBucks.UserPoints
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }
    }
}
