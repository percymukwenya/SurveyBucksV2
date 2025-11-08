using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class QuestionLogicRepository : IQuestionLogicRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public QuestionLogicRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<QuestionLogicDto>> GetQuestionLogicAsync(int questionId)
        {
            const string sql = @"
                SELECT 
                    ql.Id, ql.QuestionId, ql.LogicType, ql.ConditionType, ql.ConditionValue,
                    ql.TargetQuestionId, ql.TargetSectionId,
                    tq.Text as TargetQuestionText,
                    ts.Name as TargetSectionName
                FROM SurveyBucks.QuestionLogic ql
                LEFT JOIN SurveyBucks.Question tq ON ql.TargetQuestionId = tq.Id
                LEFT JOIN SurveyBucks.SurveySection ts ON ql.TargetSectionId = ts.Id
                WHERE ql.QuestionId = @QuestionId AND ql.IsDeleted = 0
                ORDER BY ql.CreatedDate";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<QuestionLogicDto>(sql, new { QuestionId = questionId });
            }
        }

        public async Task<IEnumerable<QuestionLogicDto>> GetSurveyLogicAsync(int surveyId)
        {
            const string sql = @"
                SELECT 
                    ql.Id, ql.QuestionId, ql.LogicType, ql.ConditionType, ql.ConditionValue,
                    ql.TargetQuestionId, ql.TargetSectionId,
                    q.Text as QuestionText,
                    tq.Text as TargetQuestionText,
                    ts.Name as TargetSectionName
                FROM SurveyBucks.QuestionLogic ql
                JOIN SurveyBucks.Question q ON ql.QuestionId = q.Id
                JOIN SurveyBucks.SurveySection ss ON q.SurveySectionId = ss.Id
                LEFT JOIN SurveyBucks.Question tq ON ql.TargetQuestionId = tq.Id
                LEFT JOIN SurveyBucks.SurveySection ts ON ql.TargetSectionId = ts.Id
                WHERE ss.SurveyId = @SurveyId AND ql.IsDeleted = 0
                ORDER BY ss.[Order], q.[Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<QuestionLogicDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<int> CreateQuestionLogicAsync(QuestionLogicCreateDto logic, string createdBy)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.QuestionLogic (
                    QuestionId, LogicType, ConditionType, ConditionValue,
                    TargetQuestionId, TargetSectionId,
                    CreatedDate, CreatedBy, IsDeleted
                ) VALUES (
                    @QuestionId, @LogicType, @ConditionType, @ConditionValue,
                    @TargetQuestionId, @TargetSectionId,
                    SYSDATETIMEOFFSET(), @CreatedBy, 0
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    logic.QuestionId,
                    logic.LogicType,
                    logic.ConditionType,
                    logic.ConditionValue,
                    logic.TargetQuestionId,
                    logic.TargetSectionId,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> UpdateQuestionLogicAsync(QuestionLogicUpdateDto logic, string modifiedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.QuestionLogic
                SET LogicType = @LogicType,
                    ConditionType = @ConditionType,
                    ConditionValue = @ConditionValue,
                    TargetQuestionId = @TargetQuestionId,
                    TargetSectionId = @TargetSectionId,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @ModifiedBy
                WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    logic.Id,
                    logic.LogicType,
                    logic.ConditionType,
                    logic.ConditionValue,
                    logic.TargetQuestionId,
                    logic.TargetSectionId,
                    ModifiedBy = modifiedBy
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteQuestionLogicAsync(int logicId, string deletedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.QuestionLogic
                SET IsDeleted = 1,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @LogicId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { LogicId = logicId, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> ValidateLogicAsync(int surveyId)
        {
            // Check for circular references and unreachable questions
            const string circularCheckSql = @"
                WITH LogicCTE AS (
                    SELECT 
                        ql.QuestionId as SourceId,
                        ql.TargetQuestionId as TargetId,
                        CAST(CONCAT(ql.QuestionId, '->', ql.TargetQuestionId) as NVARCHAR(MAX)) as Path,
                        1 as Depth
                    FROM SurveyBucks.QuestionLogic ql
                    JOIN SurveyBucks.Question q ON ql.QuestionId = q.Id
                    JOIN SurveyBucks.SurveySection ss ON q.SurveySectionId = ss.Id
                    WHERE ss.SurveyId = @SurveyId AND ql.IsDeleted = 0
                        AND ql.TargetQuestionId IS NOT NULL
                    
                    UNION ALL
                    
                    SELECT 
                        cte.SourceId,
                        ql.TargetQuestionId,
                        CAST(CONCAT(cte.Path, '->', ql.TargetQuestionId) as NVARCHAR(MAX)),
                        cte.Depth + 1
                    FROM LogicCTE cte
                    JOIN SurveyBucks.QuestionLogic ql ON cte.TargetId = ql.QuestionId
                    WHERE ql.TargetQuestionId IS NOT NULL
                        AND cte.Depth < 50 -- Prevent infinite recursion
                        AND CHARINDEX(CAST(ql.TargetQuestionId as NVARCHAR(20)), cte.Path) = 0
                )
                SELECT COUNT(*) 
                FROM LogicCTE 
                WHERE SourceId = TargetId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var hasCircularReference = await connection.ExecuteScalarAsync<int>(circularCheckSql, new { SurveyId = surveyId }) > 0;
                return !hasCircularReference;
            }
        }

        public async Task<SurveyFlowVisualizationDto> GetSurveyFlowVisualizationAsync(int surveyId)
        {
            var visualization = new SurveyFlowVisualizationDto
            {
                Nodes = new List<FlowNode>(),
                Edges = new List<FlowEdge>()
            };

            using (var connection = _connectionFactory.CreateConnection())
            {
                // Get all sections and questions
                const string nodesSql = @"
                    SELECT 
                        'S' + CAST(s.Id as VARCHAR) as Id,
                        'Section' as Type,
                        s.Name as Label,
                        s.Id as SectionId,
                        s.[Order] as DisplayOrder
                    FROM SurveyBucks.SurveySection s
                    WHERE s.SurveyId = @SurveyId AND s.IsDeleted = 0
                    
                    UNION ALL
                    
                    SELECT 
                        'Q' + CAST(q.Id as VARCHAR) as Id,
                        'Question' as Type,
                        q.Text as Label,
                        q.Id as QuestionId,
                        (s.[Order] * 1000) + q.[Order] as DisplayOrder
                    FROM SurveyBucks.Question q
                    JOIN SurveyBucks.SurveySection s ON q.SurveySectionId = s.Id
                    WHERE s.SurveyId = @SurveyId AND q.IsDeleted = 0
                    
                    ORDER BY DisplayOrder";

                var nodes = await connection.QueryAsync<dynamic>(nodesSql, new { SurveyId = surveyId });

                foreach (var node in nodes)
                {
                    visualization.Nodes.Add(new FlowNode
                    {
                        Id = node.Id,
                        Type = node.Type,
                        Label = node.Label,
                        Properties = new Dictionary<string, object> { { "data", node } }
                    });
                }

                // Get all logic connections
                const string edgesSql = @"
                    SELECT 
                        'Q' + CAST(ql.QuestionId as VARCHAR) as FromNodeId,
                        CASE 
                            WHEN ql.TargetQuestionId IS NOT NULL THEN 'Q' + CAST(ql.TargetQuestionId as VARCHAR)
                            WHEN ql.TargetSectionId IS NOT NULL THEN 'S' + CAST(ql.TargetSectionId as VARCHAR)
                            ELSE 'END'
                        END as ToNodeId,
                        ql.LogicType + ': ' + ql.ConditionType + ' ' + ql.ConditionValue as Label,
                        ql.ConditionType + ' ' + ql.ConditionValue as Condition
                    FROM SurveyBucks.QuestionLogic ql
                    JOIN SurveyBucks.Question q ON ql.QuestionId = q.Id
                    JOIN SurveyBucks.SurveySection s ON q.SurveySectionId = s.Id
                    WHERE s.SurveyId = @SurveyId AND ql.IsDeleted = 0";

                var edges = await connection.QueryAsync<FlowEdge>(edgesSql, new { SurveyId = surveyId });
                visualization.Edges = edges.ToList();

                // Add default flow edges (sequential progression)
                const string defaultFlowSql = @"
                    WITH OrderedItems AS (
                        SELECT 
                            'S' + CAST(s.Id as VARCHAR) as Id,
                            s.[Order] * 1000 as OrderValue,
                            'Section' as ItemType
                        FROM SurveyBucks.SurveySection s
                        WHERE s.SurveyId = @SurveyId AND s.IsDeleted = 0
                        
                        UNION ALL
                        
                        SELECT 
                            'Q' + CAST(q.Id as VARCHAR) as Id,
                            (s.[Order] * 1000) + q.[Order] as OrderValue,
                            'Question' as ItemType
                        FROM SurveyBucks.Question q
                        JOIN SurveyBucks.SurveySection s ON q.SurveySectionId = s.Id
                        WHERE s.SurveyId = @SurveyId AND q.IsDeleted = 0
                    )
                    SELECT 
                        o1.Id as FromNodeId,
                        o2.Id as ToNodeId
                    FROM OrderedItems o1
                    JOIN OrderedItems o2 ON o2.OrderValue = (
                        SELECT MIN(OrderValue) 
                        FROM OrderedItems 
                        WHERE OrderValue > o1.OrderValue
                    )";

                var defaultFlows = await connection.QueryAsync<(string FromNodeId, string ToNodeId)>(defaultFlowSql, new { SurveyId = surveyId });

                foreach (var flow in defaultFlows)
                {
                    // Only add if there's no existing logic edge
                    if (!visualization.Edges.Any(e => e.FromNodeId == flow.FromNodeId && e.ToNodeId == flow.ToNodeId))
                    {
                        visualization.Edges.Add(new FlowEdge
                        {
                            FromNodeId = flow.FromNodeId,
                            ToNodeId = flow.ToNodeId,
                            Label = "Next",
                            Condition = "Default"
                        });
                    }
                }
            }

            return visualization;
        }
    }
}
