using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class SurveySectionRepository : ISurveySectionRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public SurveySectionRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<SurveySectionDto>> GetSurveySectionsAsync(int surveyId)
        {
            const string sql = @"
            SELECT Id, SurveyId, Name, Description, [Order]
            FROM SurveyBucks.SurveySection
            WHERE SurveyId = @SurveyId AND IsDeleted = 0
            ORDER BY [Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<SurveySectionDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<SurveySectionDto> GetSectionByIdAsync(int sectionId)
        {
            const string sql = @"
            SELECT Id, SurveyId, Name, Description, [Order]
            FROM SurveyBucks.SurveySection
            WHERE Id = @SectionId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<SurveySectionDto>(sql, new { SectionId = sectionId });
            }
        }

        public async Task<int> CreateSectionAsync(SurveySectionCreateDto section, string createdBy)
        {
            // First, determine the maximum order value for this survey
            const string getMaxOrderSql = @"
            SELECT ISNULL(MAX([Order]), 0)
            FROM SurveyBucks.SurveySection
            WHERE SurveyId = @SurveyId AND IsDeleted = 0";

            const string insertSql = @"
            INSERT INTO SurveyBucks.SurveySection (
                SurveyId, Name, Description, [Order],
                CreatedDate, CreatedBy
            ) VALUES (
                @SurveyId, @Name, @Description, @Order,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // If Order is not specified or is 0, set it to be the next available order value
                if (section.Order <= 0)
                {
                    int maxOrder = await connection.ExecuteScalarAsync<int>(getMaxOrderSql, new { SurveyId = section.SurveyId });
                    section.Order = maxOrder + 1;
                }

                return await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    section.SurveyId,
                    section.Name,
                    section.Description,
                    section.Order,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> UpdateSectionAsync(SurveySectionUpdateDto section, string modifiedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.SurveySection
            SET Name = @Name,
                Description = @Description,
                [Order] = @Order,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    section.Id,
                    section.Name,
                    section.Description,
                    section.Order,
                    ModifiedBy = modifiedBy
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteSectionAsync(int sectionId, string deletedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.SurveySection
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @DeletedBy
            WHERE Id = @SectionId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { SectionId = sectionId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> ReorderSectionsAsync(int surveyId, IEnumerable<SectionOrderDto> sectionOrders, string modifiedBy)
        {
            const string updateOrderSql = @"
            UPDATE SurveyBucks.SurveySection
            SET [Order] = @NewOrder,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @SectionId AND SurveyId = @SurveyId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var order in sectionOrders)
                        {
                            await connection.ExecuteAsync(updateOrderSql, new
                            {
                                SectionId = order.SectionId,
                                SurveyId = surveyId,
                                NewOrder = order.NewOrder,
                                ModifiedBy = modifiedBy
                            }, transaction);
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
    }
}
