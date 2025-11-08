using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public QuestionRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<QuestionTypeDto>> GetQuestionTypesAsync()
        {
            const string sql = @"
            SELECT 
                Id, Name, Description, HasChoices, HasMinMaxValues,
                HasFreeText, HasMedia, HasMatrix
            FROM SurveyBucks.QuestionType
            WHERE IsActive = 1
            ORDER BY DisplayOrder";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<QuestionTypeDto>(sql);
            }
        }

        public async Task<IEnumerable<QuestionDto>> GetSectionQuestionsAsync(int sectionId)
        {
            const string sql = @"
            SELECT 
                q.Id, q.SurveySectionId, q.Text, q.IsMandatory, q.[Order],
                q.QuestionTypeId, qt.Name as QuestionTypeName, 
                q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText
            FROM SurveyBucks.Question q
            JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
            WHERE q.SurveySectionId = @SectionId AND q.IsDeleted = 0
            ORDER BY q.[Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<QuestionDto>(sql, new { SectionId = sectionId });
            }
        }

        public async Task<QuestionDetailDto> GetQuestionDetailsAsync(int questionId)
        {
            const string sql = @"
            SELECT 
                q.Id, q.SurveySectionId, q.Text, q.IsMandatory, q.[Order],
                q.QuestionTypeId, qt.Name as QuestionTypeName, 
                q.MinValue, q.MaxValue, q.ValidationMessage, q.HelpText,
                q.IsScreeningQuestion, q.ScreeningLogic, q.TimeoutInSeconds, q.RandomizeChoices,
                q.CreatedDate, q.CreatedBy, q.ModifiedDate, q.ModifiedBy
            FROM SurveyBucks.Question q
            JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
            WHERE q.Id = @QuestionId AND q.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var question = await connection.QuerySingleOrDefaultAsync<QuestionDetailDto>(sql, new { QuestionId = questionId });

                if (question != null)
                {
                    // Get response choices if applicable
                    if (question.QuestionTypeName == "SingleChoice" ||
                        question.QuestionTypeName == "MultipleChoice" ||
                        question.QuestionTypeName == "Dropdown")
                    {
                        question.ResponseChoices = (await GetQuestionChoicesAsync(questionId)).ToList();
                    }

                    // Get matrix rows and columns if applicable
                    if (question.QuestionTypeName == "Matrix")
                    {
                        question.MatrixRows = (await GetMatrixRowsAsync(questionId)).ToList();
                        question.MatrixColumns = (await GetMatrixColumnsAsync(questionId)).ToList();
                    }

                    // Get media attachments if applicable
                    if (question.QuestionTypeName == "Image" || question.QuestionTypeName == "FileUpload")
                    {
                        const string mediaSql = @"
                        SELECT qm.Id, qm.QuestionId, qm.MediaTypeId, mt.Name as MediaTypeName,
                            qm.FileName, qm.FileSize, qm.StoragePath, qm.DisplayOrder, qm.AltText
                        FROM SurveyBucks.QuestionMedia qm
                        JOIN SurveyBucks.MediaType mt ON qm.MediaTypeId = mt.Id
                        WHERE qm.QuestionId = @QuestionId AND qm.IsDeleted = 0
                        ORDER BY qm.DisplayOrder";

                        question.Media = (await connection.QueryAsync<QuestionMediaDto>(
                            mediaSql, new { QuestionId = questionId })).ToList();
                    }
                }

                return question;
            }
        }

        public async Task<int> CreateQuestionAsync(QuestionCreateDto question, string createdBy)
        {
            // First, determine the maximum order value for this section
            const string getMaxOrderSql = @"
            SELECT ISNULL(MAX([Order]), 0)
            FROM SurveyBucks.Question
            WHERE SurveySectionId = @SurveySectionId AND IsDeleted = 0";

            const string insertSql = @"
            INSERT INTO SurveyBucks.Question (
                SurveySectionId, Text, IsMandatory, [Order], QuestionTypeId,
                MinValue, MaxValue, ValidationMessage, HelpText,
                IsScreeningQuestion, ScreeningLogic, TimeoutInSeconds, RandomizeChoices,
                CreatedDate, CreatedBy
            ) VALUES (
                @SurveySectionId, @Text, @IsMandatory, @Order, @QuestionTypeId,
                @MinValue, @MaxValue, @ValidationMessage, @HelpText,
                @IsScreeningQuestion, @ScreeningLogic, @TimeoutInSeconds, @RandomizeChoices,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // If Order is not specified or is 0, set it to be the next available order value
                if (question.Order <= 0)
                {
                    int maxOrder = await connection.ExecuteScalarAsync<int>(getMaxOrderSql, new { SurveySectionId = question.SurveySectionId });
                    question.Order = maxOrder + 1;
                }

                return await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    question.SurveySectionId,
                    question.Text,
                    question.IsMandatory,
                    question.Order,
                    question.QuestionTypeId,
                    question.MinValue,
                    question.MaxValue,
                    question.ValidationMessage,
                    question.HelpText,
                    question.IsScreeningQuestion,
                    question.ScreeningLogic,
                    question.TimeoutInSeconds,
                    question.RandomizeChoices,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> UpdateQuestionAsync(QuestionUpdateDto question, string modifiedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.Question
            SET Text = @Text,
                IsMandatory = @IsMandatory,
                [Order] = @Order,
                QuestionTypeId = @QuestionTypeId,
                MinValue = @MinValue,
                MaxValue = @MaxValue,
                ValidationMessage = @ValidationMessage,
                HelpText = @HelpText,
                IsScreeningQuestion = @IsScreeningQuestion,
                ScreeningLogic = @ScreeningLogic,
                TimeoutInSeconds = @TimeoutInSeconds,
                RandomizeChoices = @RandomizeChoices,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    question.Id,
                    question.Text,
                    question.IsMandatory,
                    question.Order,
                    question.QuestionTypeId,
                    question.MinValue,
                    question.MaxValue,
                    question.ValidationMessage,
                    question.HelpText,
                    question.IsScreeningQuestion,
                    question.ScreeningLogic,
                    question.TimeoutInSeconds,
                    question.RandomizeChoices,
                    ModifiedBy = modifiedBy
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, string deletedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.Question
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @DeletedBy
            WHERE Id = @QuestionId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { QuestionId = questionId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> ReorderQuestionsAsync(int sectionId, IEnumerable<QuestionOrderDto> questionOrders, string modifiedBy)
        {
            const string updateOrderSql = @"
            UPDATE SurveyBucks.Question
            SET [Order] = @NewOrder,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @QuestionId AND SurveySectionId = @SectionId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var order in questionOrders)
                        {
                            await connection.ExecuteAsync(updateOrderSql, new
                            {
                                QuestionId = order.QuestionId,
                                SectionId = sectionId,
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

        public async Task<IEnumerable<QuestionResponseChoiceDto>> GetQuestionChoicesAsync(int questionId)
        {
            const string sql = @"
            SELECT Id, QuestionId, Text, Value, [Order], IsExclusiveOption
            FROM SurveyBucks.QuestionResponseChoice
            WHERE QuestionId = @QuestionId AND IsDeleted = 0
            ORDER BY [Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<QuestionResponseChoiceDto>(sql, new { QuestionId = questionId });
            }
        }

        public async Task<int> AddQuestionChoiceAsync(QuestionChoiceCreateDto choice, string createdBy)
        {
            // First, determine the maximum order value for this question
            const string getMaxOrderSql = @"
            SELECT ISNULL(MAX([Order]), 0)
            FROM SurveyBucks.QuestionResponseChoice
            WHERE QuestionId = @QuestionId AND IsDeleted = 0";

            const string insertSql = @"
            INSERT INTO SurveyBucks.QuestionResponseChoice (
                QuestionId, Text, Value, [Order], IsExclusiveOption,
                CreatedDate, CreatedBy
            ) VALUES (
                @QuestionId, @Text, @Value, @Order, @IsExclusiveOption,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // If Order is not specified or is 0, set it to be the next available order value
                if (choice.Order <= 0)
                {
                    int maxOrder = await connection.ExecuteScalarAsync<int>(getMaxOrderSql, new { QuestionId = choice.QuestionId });
                    choice.Order = maxOrder + 1;
                }

                return await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    choice.QuestionId,
                    choice.Text,
                    choice.Value,
                    choice.Order,
                    choice.IsExclusiveOption,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> UpdateQuestionChoiceAsync(QuestionChoiceUpdateDto choice, string modifiedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.QuestionResponseChoice
            SET Text = @Text,
                Value = @Value,
                [Order] = @Order,
                IsExclusiveOption = @IsExclusiveOption,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    choice.Id,
                    choice.Text,
                    choice.Value,
                    choice.Order,
                    choice.IsExclusiveOption,
                    ModifiedBy = modifiedBy
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteQuestionChoiceAsync(int choiceId, string deletedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.QuestionResponseChoice
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @DeletedBy
            WHERE Id = @ChoiceId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { ChoiceId = choiceId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<IEnumerable<MatrixRowDto>> GetMatrixRowsAsync(int questionId)
        {
            const string sql = @"
            SELECT Id, QuestionId, Text, [Order]
            FROM SurveyBucks.MatrixRows
            WHERE QuestionId = @QuestionId AND IsDeleted = 0
            ORDER BY [Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<MatrixRowDto>(sql, new { QuestionId = questionId });
            }
        }

        public async Task<IEnumerable<MatrixColumnDto>> GetMatrixColumnsAsync(int questionId)
        {
            const string sql = @"
            SELECT Id, QuestionId, Text, Value, [Order]
            FROM SurveyBucks.MatrixColumns
            WHERE QuestionId = @QuestionId AND IsDeleted = 0
            ORDER BY [Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<MatrixColumnDto>(sql, new { QuestionId = questionId });
            }
        }

        public async Task<int> AddMatrixRowAsync(MatrixRowDto row, string createdBy)
        {
            // First, determine the maximum order value for this question
            const string getMaxOrderSql = @"
            SELECT ISNULL(MAX([Order]), 0)
            FROM SurveyBucks.MatrixRows
            WHERE QuestionId = @QuestionId AND IsDeleted = 0";

            const string insertSql = @"
            INSERT INTO SurveyBucks.MatrixRows (
                QuestionId, Text, [Order],
                CreatedDate, CreatedBy
            ) VALUES (
                @QuestionId, @Text, @Order,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // If Order is not specified or is 0, set it to be the next available order value
                if (row.Order <= 0)
                {
                    int maxOrder = await connection.ExecuteScalarAsync<int>(getMaxOrderSql, new { QuestionId = row.QuestionId });
                    row.Order = maxOrder + 1;
                }

                return await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    row.QuestionId,
                    row.Text,
                    row.Order,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<int> AddMatrixColumnAsync(MatrixColumnDto column, string createdBy)
        {
            // First, determine the maximum order value for this question
            const string getMaxOrderSql = @"
            SELECT ISNULL(MAX([Order]), 0)
            FROM SurveyBucks.MatrixColumns
            WHERE QuestionId = @QuestionId AND IsDeleted = 0";

            const string insertSql = @"
            INSERT INTO SurveyBucks.MatrixColumns (
                QuestionId, Text, Value, [Order],
                CreatedDate, CreatedBy
            ) VALUES (
                @QuestionId, @Text, @Value, @Order,
                SYSDATETIMEOFFSET(), @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                // If Order is not specified or is 0, set it to be the next available order value
                if (column.Order <= 0)
                {
                    int maxOrder = await connection.ExecuteScalarAsync<int>(getMaxOrderSql, new { QuestionId = column.QuestionId });
                    column.Order = maxOrder + 1;
                }

                return await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    column.QuestionId,
                    column.Text,
                    column.Value,
                    column.Order,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> DeleteMatrixRowAsync(int rowId, string deletedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.MatrixRows
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @DeletedBy
            WHERE Id = @RowId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { RowId = rowId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> DeleteMatrixColumnAsync(int columnId, string deletedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.MatrixColumns
            SET IsDeleted = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @DeletedBy
            WHERE Id = @ColumnId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { ColumnId = columnId, DeletedBy = deletedBy });
                return result > 0;
            }
        }
    }
}
