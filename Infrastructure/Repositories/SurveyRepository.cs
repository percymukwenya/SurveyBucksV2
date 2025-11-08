using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SurveyRepository : ISurveyRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public SurveyRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<SurveyDto>> GetActiveSurveysAsync()
        {
            const string sql = @"
            SELECT Id, Name, Description, OpeningDateTime, ClosingDateTime,
                   DurationInSeconds, CompanyName, CompanyDescription, Industry,
                   MinQuestions, MaxTimeInMins, RequireAllQuestions,
                   IsActive, IsPublished, IsDeleted
            FROM SurveyBucks.Survey
            WHERE IsActive = 1 AND IsPublished = 1 AND IsDeleted = 0
              AND GETDATE() BETWEEN OpeningDateTime AND ClosingDateTime";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<SurveyDto>(sql);
        }

        public async Task<SurveyDto> GetByIdAsync(int surveyId)
        {
            const string sql = @"
            SELECT Id, Name, Description, OpeningDateTime, ClosingDateTime,
                   DurationInSeconds, CompanyName, CompanyDescription, Industry,
                   MinQuestions, MaxTimeInMins, RequireAllQuestions,
                   IsActive, IsPublished, IsDeleted
            FROM SurveyBucks.Survey
            WHERE Id = @SurveyId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<SurveyDto>(sql, new { SurveyId = surveyId });
        }

        public async Task<bool> HasUserParticipatedAsync(int surveyId, string userId)
        {
            const string sql = @"
            SELECT COUNT(1) 
            FROM SurveyBucks.SurveyParticipation
            WHERE SurveyId = @SurveyId AND UserId = @UserId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { SurveyId = surveyId, UserId = userId });
            return count > 0;
        }
    }
}
