using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Infrastructure.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserTargetingRepository : IUserTargetingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserTargetingRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SurveyTargetingDto> GetSurveyTargetingAsync(int surveyId)
        {
            var targeting = new SurveyTargetingDto();

            using var connection = _connectionFactory.CreateConnection();

            // Get age ranges
            const string ageRangesSql = @"
            SELECT MinAge, MaxAge 
            FROM SurveyBucks.AgeRangeTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.AgeRanges = (await connection.QueryAsync<AgeRangeDto>(ageRangesSql, new { SurveyId = surveyId })).ToList();

            // Get gender targets
            const string gendersSql = @"
            SELECT Gender 
            FROM SurveyBucks.GenderTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.Genders = (await connection.QueryAsync<string>(gendersSql, new { SurveyId = surveyId })).ToList();

            // Get location targets
            const string locationsSql = @"
            SELECT Country, Location 
            FROM SurveyBucks.LocationTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.Locations = (await connection.QueryAsync<LocationTargetDto>(locationsSql, new { SurveyId = surveyId })).ToList();

            // Get income ranges
            const string incomeRangesSql = @"
            SELECT MinIncome, MaxIncome 
            FROM SurveyBucks.IncomeRangeTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.IncomeRanges = (await connection.QueryAsync<IncomeRangeDto>(incomeRangesSql, new { SurveyId = surveyId })).ToList();

            // Get education targets
            const string educationSql = @"
            SELECT Education 
            FROM SurveyBucks.EducationTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.EducationLevels = (await connection.QueryAsync<string>(educationSql, new { SurveyId = surveyId })).ToList();

            // Get interest targets
            const string interestsSql = @"
            SELECT Interest 
            FROM SurveyBucks.InterestTargets 
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";
            targeting.Interests = (await connection.QueryAsync<string>(interestsSql, new { SurveyId = surveyId })).ToList();

            return targeting;
        }
    }
}
