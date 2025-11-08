using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class DemographicTargetingRepository : IDemographicTargetingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public DemographicTargetingRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<AgeRangeTargetDto>> GetAgeRangeTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, MinAge, MaxAge
                FROM SurveyBucks.AgeRangeTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<AgeRangeTargetDto>(sql, new { SurveyId = surveyId });
        }

        public async Task<IEnumerable<GenderTargetDto>> GetGenderTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, Gender
                FROM SurveyBucks.GenderTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<GenderTargetDto>(sql, new { SurveyId = surveyId });
        }

        public async Task<IEnumerable<EducationTargetDto>> GetEducationTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, Education
                FROM SurveyBucks.EducationTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<EducationTargetDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<IEnumerable<IncomeRangeTargetDto>> GetIncomeRangeTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, MinIncome, MaxIncome
                FROM SurveyBucks.IncomeRangeTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<IncomeRangeTargetDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<IEnumerable<InterestTargetDto>> GetInterestTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, Interest, MinInterestLevel
                FROM SurveyBucks.InterestTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<InterestTargetDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<IEnumerable<LocationTargetDto>> GetLocationTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, Location
                FROM SurveyBucks.LocationTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<LocationTargetDto>(sql, new { SurveyId = surveyId });
        }

        public async Task<IEnumerable<OccupationTargetDto>> GetOccupationTargetsAsync(int surveyId)
        {
            const string sql = @"
                SELECT Id, SurveyId, Occupation
                FROM SurveyBucks.OccupationTargets
                WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<OccupationTargetDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<int> AddTargetAsync<T>(T target, string createdBy) where T : class
        {
            string tableName = GetTableNameForType<T>();
            string sql = GenerateInsertSqlForType<T>(tableName, createdBy);

            using (var connection = _connectionFactory.CreateConnection())
            {
                // Add CreatedBy parameter to the target object
                var parameterObject = new DynamicParameters(target);
                parameterObject.Add("CreatedBy", createdBy);

                // Explicitly cast connection to use extension method
                var dbConnection = connection;
                return await SqlMapper.ExecuteScalarAsync<int>(dbConnection, sql, parameterObject);
            }
        }

        public async Task<bool> ClearAllTargetsAsync(int surveyId, string deletedBy)
        {
            string[] targetTables = {
            "SurveyBucks.AgeRangeTargets",
            "SurveyBucks.GenderTargets",
            "SurveyBucks.LocationTargets",
            "SurveyBucks.EducationTargets",
            "SurveyBucks.OccupationTargets",
            "SurveyBucks.IncomeRangeTargets",
            "SurveyBucks.InterestTargets"
        };

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (string tableName in targetTables)
                        {
                            string sql = $@"
                        UPDATE {tableName}
                        SET IsDeleted = 1,
                            ModifiedDate = SYSDATETIMEOFFSET(),
                            ModifiedBy = @DeletedBy
                        WHERE SurveyId = @SurveyId AND IsDeleted = 0";

                            await connection.ExecuteAsync(sql, new { SurveyId = surveyId, DeletedBy = deletedBy }, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> DeleteTargetAsync(string targetType, int targetId, string deletedBy)
        {
            string tableName = GetTableNameForTargetType(targetType);

            string sql = $@"
                UPDATE {tableName}
                SET IsDeleted = 1,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @TargetId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                int result = await connection.ExecuteAsync(sql, new { TargetId = targetId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> UpdateTargetAsync<T>(T target, string modifiedBy) where T : class
        {
            string tableName = GetTableNameForType<T>();
            string sql = GenerateUpdateSqlForType<T>(tableName, modifiedBy);

            using (var connection = _connectionFactory.CreateConnection())
            {
                int result = await connection.ExecuteAsync(sql, target);
                return result > 0;
            }
        }

        private string GetTableNameForType<T>() where T : class
        {
            if (typeof(T) == typeof(AgeRangeTargetDto))
                return "SurveyBucks.AgeRangeTargets";
            if (typeof(T) == typeof(GenderTargetDto))
                return "SurveyBucks.GenderTargets";
            if (typeof(T) == typeof(LocationTargetDto))
                return "SurveyBucks.LocationTargets";
            if (typeof(T) == typeof(EducationTargetDto))
                return "SurveyBucks.EducationTargets";
            if (typeof(T) == typeof(OccupationTargetDto))
                return "SurveyBucks.OccupationTargets";
            if (typeof(T) == typeof(IncomeRangeTargetDto))
                return "SurveyBucks.IncomeRangeTargets";
            if (typeof(T) == typeof(InterestTargetDto))
                return "SurveyBucks.InterestTargets";

            throw new ArgumentException($"Unsupported target type: {typeof(T).Name}");
        }

        private string GetTableNameForTargetType(string targetType)
        {
            switch (targetType.ToLower())
            {
                case "agerange":
                    return "SurveyBucks.AgeRangeTargets";
                case "gender":
                    return "SurveyBucks.GenderTargets";
                case "location":
                    return "SurveyBucks.LocationTargets";
                case "education":
                    return "SurveyBucks.EducationTargets";
                case "occupation":
                    return "SurveyBucks.OccupationTargets";
                case "incomerange":
                    return "SurveyBucks.IncomeRangeTargets";
                case "interest":
                    return "SurveyBucks.InterestTargets";
                default:
                    throw new ArgumentException($"Unsupported target type: {targetType}");
            }
        }

        private string GenerateInsertSqlForType<T>(string tableName, string createdBy) where T : class
        {
            if (typeof(T) == typeof(AgeRangeTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, MinAge, MaxAge, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @MinAge, @MaxAge, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(GenderTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, Gender, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @Gender, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(LocationTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, Location, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @Location, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(EducationTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, Education, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @Education, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(OccupationTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, Occupation, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @Occupation, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(IncomeRangeTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, MinIncome, MaxIncome, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @MinIncome, @MaxIncome, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            if (typeof(T) == typeof(InterestTargetDto))
            {
                return $@"
                    INSERT INTO {tableName} (SurveyId, Interest, MinInterestLevel, CreatedDate, CreatedBy)
                    VALUES (@SurveyId, @Interest, @MinInterestLevel, SYSDATETIMEOFFSET(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            }

            throw new ArgumentException($"Unsupported target type: {typeof(T).Name}");
        }

        private string GenerateUpdateSqlForType<T>(string tableName, string modifiedBy) where T : class
        {
            if (typeof(T) == typeof(AgeRangeTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET MinAge = @MinAge,
                        MaxAge = @MaxAge,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(GenderTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET Gender = @Gender,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(LocationTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET Location = @Location,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(EducationTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET Education = @Education,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(OccupationTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET Occupation = @Occupation,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(IncomeRangeTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET MinIncome = @MinIncome,
                        MaxIncome = @MaxIncome,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            if (typeof(T) == typeof(InterestTargetDto))
            {
                return $@"
                    UPDATE {tableName}
                    SET Interest = @Interest,
                        MinInterestLevel = @MinInterestLevel,
                        ModifiedDate = SYSDATETIMEOFFSET(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsDeleted = 0";
            }

            throw new ArgumentException($"Unsupported target type: {typeof(T).Name}");
        }
    }
}
