using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Infrastructure.Shared;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserDto> GetUserByIdAsync(string userId)
        {
            const string sql = @"
            SELECT FirstName, LastName, Email
            FROM SurveyBucks.Users
            WHERE UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<UserDto>(sql, new { UserId = userId });
        }

        public async Task<string> GetUserIdByEmailAsync(string email)
        {
            const string sql = "SELECT Id FROM SurveyBucks.Users WHERE Email = @Email";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { Email = email });
        }

        public async Task<bool> DeleteUserWithRolesAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var transaction = connection.BeginTransaction();

            const string sql = @"
                DECLARE @UserId NVARCHAR(450)
                SELECT @UserId = Id FROM AspNetUsers WHERE Email = @Email
        
                IF @UserId IS NOT NULL
                BEGIN
                    DELETE FROM SurveyBucks.Roles WHERE UserId = @UserId
                    DELETE FROM SurveyBucks.UserClaims WHERE UserId = @UserId  
                    DELETE FROM SurveyBucks.UserLogins WHERE UserId = @UserId
                    DELETE FROM SurveyBucks.UserTokens WHERE UserId = @UserId
                    DELETE FROM SurveyBucks.Users WHERE Id = @UserId
                    SELECT 1 as Success
                END
                ELSE
                    SELECT 0 as Success";

            var result = await connection.QuerySingleOrDefaultAsync<int>(sql, new { Email = email });
            return result == 1;
        }
    }
}
