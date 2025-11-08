using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public AnalyticsRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(int surveyId)
        {
            const string sql = @"
            SELECT 
                sa.SurveyId,
                s.Name AS SurveyName,
                sa.TotalViews,
                sa.TotalStarts,
                sa.TotalCompletions,
                sa.CompletionRate,
                sa.AverageCompletionTimeSeconds,
                sa.DropOffRate,
                sa.TotalDisqualifications,
                sa.LastUpdated
            FROM SurveyBucks.SurveyAnalytics sa
            JOIN SurveyBucks.Survey s ON sa.SurveyId = s.Id
            WHERE sa.SurveyId = @SurveyId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var analytics = await connection.QuerySingleOrDefaultAsync<SurveyAnalyticsDto>(sql, new { SurveyId = surveyId });

                if (analytics != null)
                {
                    // Get completions by day
                    const string completionsByDaySql = @"
                    SELECT 
                        FORMAT(CompletedAtDateTime, 'yyyy-MM-dd') AS [Day],
                        COUNT(*) AS Count
                    FROM SurveyBucks.SurveyParticipation
                    WHERE SurveyId = @SurveyId
                      AND StatusId IN (3, 7) -- Completed or Rewarded
                      AND CompletedAtDateTime IS NOT NULL
                      AND IsDeleted = 0
                    GROUP BY FORMAT(CompletedAtDateTime, 'yyyy-MM-dd')
                    ORDER BY [Day]";

                    var completionsByDay = await connection.QueryAsync<(string Day, int Count)>(completionsByDaySql, new { SurveyId = surveyId });
                    analytics.CompletionsByDay = completionsByDay.ToDictionary(x => x.Day, x => x.Count);

                    // Get average time by section
                    const string sectionTimeSql = @"
                    SELECT 
                        ss.Name,
                        AVG(sa.AverageTimeInSectionSeconds) AS AverageTime
                    FROM SurveyBucks.SectionAnalytics sa
                    JOIN SurveyBucks.SurveySection ss ON sa.SurveySectionId = ss.Id
                    WHERE ss.SurveyId = @SurveyId
                    GROUP BY ss.Name";

                    var sectionTimes = await connection.QueryAsync<(string Name, decimal AverageTime)>(sectionTimeSql, new { SurveyId = surveyId });
                    analytics.AverageTimeBySection = sectionTimes.ToDictionary(x => x.Name, x => x.AverageTime);

                    // Get participations by status
                    const string statusSql = @"
                    SELECT 
                        ps.Name AS Status,
                        COUNT(*) AS Count
                    FROM SurveyBucks.SurveyParticipation sp
                    JOIN SurveyBucks.ParticipationStatus ps ON sp.StatusId = ps.Id
                    WHERE sp.SurveyId = @SurveyId AND sp.IsDeleted = 0
                    GROUP BY ps.Name";

                    var statuses = await connection.QueryAsync<(string Status, int Count)>(statusSql, new { SurveyId = surveyId });
                    analytics.ParticipationsByStatus = statuses.ToDictionary(x => x.Status, x => x.Count);
                }

                return analytics;
            }
        }

        public async Task<IEnumerable<QuestionAnalyticsDto>> GetQuestionAnalyticsAsync(int surveyId)
        {
            const string sql = @"
            SELECT 
                qa.QuestionId,
                q.Text AS QuestionText,
                qt.Name AS QuestionType,
                ss.Id AS SectionId,
                ss.Name AS SectionName,
                qa.TotalResponses,
                qa.AverageTimeToAnswerSeconds,
                qa.SkipRate
            FROM SurveyBucks.QuestionAnalytics qa
            JOIN SurveyBucks.Question q ON qa.QuestionId = q.Id
            JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
            JOIN SurveyBucks.SurveySection ss ON q.SurveySectionId = ss.Id
            WHERE ss.SurveyId = @SurveyId
            ORDER BY ss.[Order], q.[Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var questionAnalytics = await connection.QueryAsync<QuestionAnalyticsDto>(sql, new { SurveyId = surveyId });

                // For each question, get the response distribution
                foreach (var question in questionAnalytics)
                {
                    var distributionSql = GetResponseDistributionSql(question.QuestionType);
                    if (!string.IsNullOrEmpty(distributionSql))
                    {
                        var distribution = await connection.QueryAsync<(string Value, int Count)>(
                            distributionSql, new { QuestionId = question.QuestionId });

                        question.ResponseDistribution = distribution.ToDictionary(x => x.Value, x => x.Count);
                    }
                }

                return questionAnalytics;
            }
        }

        private string GetResponseDistributionSql(string questionType)
        {
            switch (questionType)
            {
                case "SingleChoice":
                case "MultipleChoice":
                case "Dropdown":
                    return @"
                    SELECT 
                        qrc.Text AS Value,
                        COUNT(*) AS Count
                    FROM SurveyBucks.SurveyResponse sr
                    JOIN SurveyBucks.QuestionResponseChoice qrc ON sr.Answer = qrc.Id::varchar
                    WHERE sr.QuestionId = @QuestionId AND sr.IsDeleted = 0
                    GROUP BY qrc.Text
                    ORDER BY Count DESC";

                case "YesNo":
                    return @"
                    SELECT 
                        sr.Answer AS Value,
                        COUNT(*) AS Count
                    FROM SurveyBucks.SurveyResponse sr
                    WHERE sr.QuestionId = @QuestionId AND sr.IsDeleted = 0
                    GROUP BY sr.Answer
                    ORDER BY Count DESC";

                case "Rating":
                case "Slider":
                    return @"
                    SELECT 
                        sr.Answer AS Value,
                        COUNT(*) AS Count
                    FROM SurveyBucks.SurveyResponse sr
                    WHERE sr.QuestionId = @QuestionId AND sr.IsDeleted = 0
                    GROUP BY sr.Answer
                    ORDER BY sr.Answer";

                default:
                    return null; // No distribution for text-based questions
            }
        }

        public async Task<IEnumerable<SectionAnalyticsDto>> GetSectionAnalyticsAsync(int surveyId)
        {
            const string sql = @"
            SELECT 
                sa.SectionId,
                ss.Name AS SectionName,
                sa.TotalEntered,
                sa.TotalCompleted,
                sa.CompletionRate,
                sa.AverageTimeInSectionSeconds,
                sa.DropOffRate
            FROM SurveyBucks.SectionAnalytics sa
            JOIN SurveyBucks.SurveySection ss ON sa.SurveySectionId = ss.Id
            WHERE ss.SurveyId = @SurveyId
            ORDER BY ss.[Order]";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<SectionAnalyticsDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<IEnumerable<DemographicBreakdownDto>> GetResponseDemographicsAsync(int surveyId)
        {
            var result = new List<DemographicBreakdownDto>();

            using (var connection = _connectionFactory.CreateConnection())
            {
                // Age breakdown
                const string ageSql = @"
                SELECT 
                    CASE 
                        WHEN d.Age < 18 THEN 'Under 18'
                        WHEN d.Age BETWEEN 18 AND 24 THEN '18-24'
                        WHEN d.Age BETWEEN 25 AND 34 THEN '25-34'
                        WHEN d.Age BETWEEN 35 AND 44 THEN '35-44'
                        WHEN d.Age BETWEEN 45 AND 54 THEN '45-54'
                        WHEN d.Age BETWEEN 55 AND 64 THEN '55-64'
                        ELSE '65+' 
                    END AS Value,
                    COUNT(*) AS Count
                FROM SurveyBucks.SurveyParticipation sp
                JOIN SurveyBucks.Demographics d ON sp.UserId = d.UserId
                WHERE sp.SurveyId = @SurveyId 
                  AND sp.StatusId IN (3, 7) -- Completed or Rewarded
                  AND sp.IsDeleted = 0
                  AND d.IsDeleted = 0
                GROUP BY CASE 
                        WHEN d.Age < 18 THEN 'Under 18'
                        WHEN d.Age BETWEEN 18 AND 24 THEN '18-24'
                        WHEN d.Age BETWEEN 25 AND 34 THEN '25-34'
                        WHEN d.Age BETWEEN 35 AND 44 THEN '35-44'
                        WHEN d.Age BETWEEN 45 AND 54 THEN '45-54'
                        WHEN d.Age BETWEEN 55 AND 64 THEN '55-64'
                        ELSE '65+' 
                     END
                ORDER BY Value";

                var ageBreakdown = await connection.QueryAsync<(string Value, int Count)>(ageSql, new { SurveyId = surveyId });
                var totalAge = ageBreakdown.Sum(x => x.Count);

                foreach (var (value, count) in ageBreakdown)
                {
                    result.Add(new DemographicBreakdownDto
                    {
                        Category = "Age",
                        Value = value,
                        Count = count,
                        Percentage = totalAge > 0 ? (decimal)count / totalAge * 100 : 0
                    });
                }

                // Gender breakdown
                const string genderSql = @"
                SELECT 
                    d.Gender AS Value,
                    COUNT(*) AS Count
                FROM SurveyBucks.SurveyParticipation sp
                JOIN SurveyBucks.Demographics d ON sp.UserId = d.UserId
                WHERE sp.SurveyId = @SurveyId 
                  AND sp.StatusId IN (3, 7) -- Completed or Rewarded
                  AND sp.IsDeleted = 0
                  AND d.IsDeleted = 0
                GROUP BY d.Gender
                ORDER BY Count DESC";

                var genderBreakdown = await connection.QueryAsync<(string Value, int Count)>(genderSql, new { SurveyId = surveyId });
                var totalGender = genderBreakdown.Sum(x => x.Count);

                foreach (var (value, count) in genderBreakdown)
                {
                    result.Add(new DemographicBreakdownDto
                    {
                        Category = "Gender",
                        Value = value,
                        Count = count,
                        Percentage = totalGender > 0 ? (decimal)count / totalGender * 100 : 0
                    });
                }

                // Location breakdown
                const string locationSql = @"
                SELECT 
                    d.Country AS Value,
                    COUNT(*) AS Count
                FROM SurveyBucks.SurveyParticipation sp
                JOIN SurveyBucks.Demographics d ON sp.UserId = d.UserId
                WHERE sp.SurveyId = @SurveyId 
                  AND sp.StatusId IN (3, 7) -- Completed or Rewarded
                  AND sp.IsDeleted = 0
                  AND d.IsDeleted = 0
                GROUP BY d.Country
                ORDER BY Count DESC";

                var locationBreakdown = await connection.QueryAsync<(string Value, int Count)>(locationSql, new { SurveyId = surveyId });
                var totalLocation = locationBreakdown.Sum(x => x.Count);

                foreach (var (value, count) in locationBreakdown)
                {
                    result.Add(new DemographicBreakdownDto
                    {
                        Category = "Country",
                        Value = value,
                        Count = count,
                        Percentage = totalLocation > 0 ? (decimal)count / totalLocation * 100 : 0
                    });
                }

                // Add more demographic breakdowns as needed (education, income, etc.)
            }

            return result;
        }

        public async Task<IEnumerable<ResponseSummaryDto>> GetQuestionResponseSummaryAsync(int questionId)
        {
            const string sql = @"
            SELECT 
                q.Id AS QuestionId,
                q.Text AS QuestionText,
                CASE 
                    WHEN qt.Name IN ('SingleChoice', 'MultipleChoice', 'Dropdown') THEN qrc.Text
                    ELSE sr.Answer
                END AS ResponseValue,
                COUNT(*) AS ResponseCount,
                COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SurveyBucks.SurveyResponse WHERE QuestionId = @QuestionId AND IsDeleted = 0) AS ResponsePercentage
            FROM SurveyBucks.SurveyResponse sr
            JOIN SurveyBucks.Question q ON sr.QuestionId = q.Id
            JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
            LEFT JOIN SurveyBucks.QuestionResponseChoice qrc ON 
                CASE WHEN ISNUMERIC(sr.Answer) = 1 THEN CAST(sr.Answer AS INT) ELSE NULL END = qrc.Id
                AND qt.Name IN ('SingleChoice', 'MultipleChoice', 'Dropdown')
            WHERE sr.QuestionId = @QuestionId AND sr.IsDeleted = 0
            GROUP BY q.Id, q.Text, 
                CASE 
                    WHEN qt.Name IN ('SingleChoice', 'MultipleChoice', 'Dropdown') THEN qrc.Text
                    ELSE sr.Answer
                END
            ORDER BY ResponseCount DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<ResponseSummaryDto>(sql, new { QuestionId = questionId });
            }
        }

        public async Task<IEnumerable<ResponseTrendDto>> GetResponseTrendsAsync(int surveyId, string timeFrame)
        {
            string groupByFormat;

            switch (timeFrame?.ToLower())
            {
                case "hourly":
                    groupByFormat = "yyyy-MM-dd HH:00";
                    break;
                case "weekly":
                    groupByFormat = "yyyy-'W'ww"; // Year-Week format
                    break;
                case "monthly":
                    groupByFormat = "yyyy-MM";
                    break;
                default:
                    groupByFormat = "yyyy-MM-dd"; // Default to daily
                    break;
            }

            string sql = $@"
            SELECT 
                FORMAT(sr.ResponseDateTime, '{groupByFormat}') AS Date,
                COUNT(*) AS Responses,
                COUNT(DISTINCT sr.SurveyParticipationId) AS Completions,
                AVG(rt.TimeSpentInSeconds) AS AverageCompletionTime
            FROM SurveyBucks.SurveyResponse sr
            JOIN SurveyBucks.SurveyParticipation sp ON sr.SurveyParticipationId = sp.Id
            LEFT JOIN SurveyBucks.ResponseTimeTracking rt ON rt.SurveyParticipationId = sp.Id AND rt.QuestionId = sr.QuestionId
            WHERE sp.SurveyId = @SurveyId
              AND sr.IsDeleted = 0
              AND sp.IsDeleted = 0
            GROUP BY FORMAT(sr.ResponseDateTime, '{groupByFormat}')
            ORDER BY Date";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<ResponseTrendDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<PlatformMetricsDto> GetPlatformMetricsAsync()
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                // Get basic counts
                const string metricsSql = @"
                SELECT
                    (SELECT COUNT(*) FROM AspNetUsers WHERE IsActive = 1) AS TotalUsers,
                    (SELECT COUNT(*) FROM AspNetUsers WHERE LastLoginDate >= DATEADD(DAY, -30, GETDATE())) AS ActiveUsers,
                    (SELECT COUNT(*) FROM SurveyBucks.Survey WHERE IsDeleted = 0) AS TotalSurveys,
                    (SELECT COUNT(*) FROM SurveyBucks.Survey WHERE IsActive = 1 AND IsPublished = 1 AND IsDeleted = 0) AS ActiveSurveys,
                    (SELECT COUNT(*) FROM SurveyBucks.SurveyParticipation WHERE IsDeleted = 0) AS TotalParticipations,
                    (SELECT COUNT(*) FROM SurveyBucks.SurveyParticipation WHERE StatusId IN (3, 7) AND IsDeleted = 0) AS TotalCompletions,
                    (SELECT SUM(Amount) FROM SurveyBucks.PointTransactions WHERE TransactionType = 'Earned' AND IsDeleted = 0) AS TotalPointsAwarded,
                    (SELECT COUNT(*) FROM SurveyBucks.UserRewards WHERE IsDeleted = 0) AS TotalRewardsRedeemed,
                    (SELECT AVG(TotalLogins * 1.0 / DATEDIFF(DAY, RegistrationDate, GETDATE())) 
                     FROM SurveyBucks.UserEngagement ue
                     JOIN AspNetUsers u ON ue.UserId = u.Id
                     WHERE ue.IsDeleted = 0 AND DATEDIFF(DAY, RegistrationDate, GETDATE()) > 0) AS AverageUserEngagement";

                var metrics = await connection.QuerySingleAsync<PlatformMetricsDto>(metricsSql);

                // Calculate completion rate
                if (metrics.TotalParticipations > 0)
                {
                    metrics.OverallCompletionRate = (decimal)metrics.TotalCompletions / metrics.TotalParticipations * 100;
                }

                // Get users by level distribution
                const string levelSql = @"
                SELECT 
                    ul.Name AS LevelName,
                    COUNT(*) AS UserCount
                FROM SurveyBucks.UserPoints up
                JOIN SurveyBucks.UserLevels ul ON up.PointsLevel = ul.Level
                WHERE up.IsDeleted = 0
                GROUP BY ul.Name, ul.Level
                ORDER BY ul.Level";

                var levelDistribution = await connection.QueryAsync<(string LevelName, int UserCount)>(levelSql);
                metrics.UsersByLevel = levelDistribution.ToDictionary(x => x.LevelName, x => x.UserCount);

                // Get participation trend by day (last 30 days)
                const string participationTrendSql = @"
                SELECT 
                    FORMAT(CreatedDate, 'yyyy-MM-dd') AS [Day],
                    COUNT(*) AS Count
                FROM SurveyBucks.SurveyParticipation
                WHERE CreatedDate >= DATEADD(DAY, -30, GETDATE())
                  AND IsDeleted = 0
                GROUP BY FORMAT(CreatedDate, 'yyyy-MM-dd')
                ORDER BY [Day]";

                var participationTrend = await connection.QueryAsync<(string Day, int Count)>(participationTrendSql);
                metrics.ParticipationsByDay = participationTrend.ToDictionary(x => x.Day, x => x.Count);

                // Get completion trend by day (last 30 days)
                const string completionTrendSql = @"
                SELECT 
                    FORMAT(CompletedAtDateTime, 'yyyy-MM-dd') AS [Day],
                    COUNT(*) AS Count
                FROM SurveyBucks.SurveyParticipation
                WHERE CompletedAtDateTime >= DATEADD(DAY, -30, GETDATE())
                  AND StatusId IN (3, 7) -- Completed or Rewarded
                  AND IsDeleted = 0
                GROUP BY FORMAT(CompletedAtDateTime, 'yyyy-MM-dd')
                ORDER BY [Day]";

                var completionTrend = await connection.QueryAsync<(string Day, int Count)>(completionTrendSql);
                metrics.CompletionsByDay = completionTrend.ToDictionary(x => x.Day, x => x.Count);

                return metrics;
            }
        }

        public async Task<IEnumerable<UserAdminDto>> GetTopUsersAsync(string metric, int take = 10)
        {
            string sql;

            switch (metric?.ToLower())
            {
                case "points":
                    sql = @"
                    SELECT TOP(@Take)
                        u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive,
                        u.RegistrationDate, u.LastLoginDate,
                        ue.TotalSurveysCompleted, up.TotalPoints, up.PointsLevel,
                        CASE WHEN dps.CompletionPercentage >= 100 THEN 1 ELSE 0 END AS ProfileComplete
                    FROM AspNetUsers u
                    LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
                    LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
                    LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
                    WHERE u.IsActive = 1
                    ORDER BY up.TotalPoints DESC";
                    break;

                case "surveys":
                    sql = @"
                    SELECT TOP(@Take)
                        u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive,
                        u.RegistrationDate, u.LastLoginDate,
                        ue.TotalSurveysCompleted, up.TotalPoints, up.PointsLevel,
                        CASE WHEN dps.CompletionPercentage >= 100 THEN 1 ELSE 0 END AS ProfileComplete
                    FROM AspNetUsers u
                    LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
                    LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
                    LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
                    WHERE u.IsActive = 1
                    ORDER BY ue.TotalSurveysCompleted DESC";
                    break;

                case "login":
                    sql = @"
                    SELECT TOP(@Take)
                        u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive,
                        u.RegistrationDate, u.LastLoginDate,
                        ue.TotalSurveysCompleted, up.TotalPoints, up.PointsLevel,
                        CASE WHEN dps.CompletionPercentage >= 100 THEN 1 ELSE 0 END AS ProfileComplete
                    FROM AspNetUsers u
                    LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
                    LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
                    LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
                    WHERE u.IsActive = 1
                    ORDER BY ue.CurrentLoginStreak DESC";
                    break;

                case "recent":
                default:
                    sql = @"
                    SELECT TOP(@Take)
                        u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive,
                        u.RegistrationDate, u.LastLoginDate,
                        ue.TotalSurveysCompleted, up.TotalPoints, up.PointsLevel,
                        CASE WHEN dps.CompletionPercentage >= 100 THEN 1 ELSE 0 END AS ProfileComplete
                    FROM AspNetUsers u
                    LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
                    LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
                    LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
                    WHERE u.IsActive = 1
                    ORDER BY u.LastLoginDate DESC";
                    break;
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<UserAdminDto>(sql, new { Take = take });
            }
        }

        public async Task<IEnumerable<SurveyAnalyticsSummaryDto>> GetTopSurveysAsync(string metric, int take = 10)
        {
            string sql;

            switch (metric?.ToLower())
            {
                case "completions":
                    sql = @"
                    SELECT TOP(@Take)
                        s.Id, s.Name, s.OpeningDateTime, s.ClosingDateTime,
                        sa.TotalCompletions, sa.CompletionRate,
                        sa.AverageCompletionTimeSeconds,
                        (SELECT COUNT(*) FROM SurveyBucks.SurveyParticipation 
                         WHERE SurveyId = s.Id AND IsDeleted = 0) AS TotalParticipations
                    FROM SurveyBucks.Survey s
                    JOIN SurveyBucks.SurveyAnalytics sa ON s.Id = sa.SurveyId
                    WHERE s.IsDeleted = 0
                    ORDER BY sa.TotalCompletions DESC";
                    break;

                case "rate":
                    sql = @"
                    SELECT TOP(@Take)
                        s.Id, s.Name, s.OpeningDateTime, s.ClosingDateTime,
                        sa.TotalCompletions, sa.CompletionRate,
                        sa.AverageCompletionTimeSeconds,
                        (SELECT COUNT(*) FROM SurveyBucks.SurveyParticipation 
                         WHERE SurveyId = s.Id AND IsDeleted = 0) AS TotalParticipations
                    FROM SurveyBucks.Survey s
                    JOIN SurveyBucks.SurveyAnalytics sa ON s.Id = sa.SurveyId
                    WHERE s.IsDeleted = 0 AND sa.TotalStarts > 10 -- Minimum threshold for meaningful rate
                    ORDER BY sa.CompletionRate DESC";
                    break;

                case "popular":
                default:
                    sql = @"
                    SELECT TOP(@Take)
                        s.Id, s.Name, s.OpeningDateTime, s.ClosingDateTime,
                        sa.TotalCompletions, sa.CompletionRate,
                        sa.AverageCompletionTimeSeconds,
                        (SELECT COUNT(*) FROM SurveyBucks.SurveyParticipation 
                         WHERE SurveyId = s.Id AND IsDeleted = 0) AS TotalParticipations
                    FROM SurveyBucks.Survey s
                    JOIN SurveyBucks.SurveyAnalytics sa ON s.Id = sa.SurveyId
                    WHERE s.IsDeleted = 0
                    ORDER BY sa.TotalStarts DESC";
                    break;
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<SurveyAnalyticsSummaryDto>(sql, new { Take = take });
            }
        }

        public async Task<IEnumerable<DemographicBreakdownDto>> GetUserDemographicsBreakdownAsync(string demographicType)
        {
            string sql;

            switch (demographicType?.ToLower())
            {
                case "gender":
                    sql = @"
                    SELECT 
                        'Gender' AS Category,
                        Gender AS Value,
                        COUNT(*) AS Count,
                        COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SurveyBucks.Demographics WHERE IsDeleted = 0) AS Percentage
                    FROM SurveyBucks.Demographics
                    WHERE IsDeleted = 0
                    GROUP BY Gender
                    ORDER BY Count DESC";
                    break;

                case "age":
                    sql = @"
                    SELECT 
                        'Age' AS Category,
                        CASE 
                            WHEN Age < 18 THEN 'Under 18'
                            WHEN Age BETWEEN 18 AND 24 THEN '18-24'
                            WHEN Age BETWEEN 25 AND 34 THEN '25-34'
                            WHEN Age BETWEEN 35 AND 44 THEN '35-44'
                            WHEN Age BETWEEN 45 AND 54 THEN '45-54'
                            WHEN Age BETWEEN 55 AND 64 THEN '55-64'
                            ELSE '65+' 
                        END AS Value,
                        COUNT(*) AS Count,
                        COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SurveyBucks.Demographics WHERE IsDeleted = 0) AS Percentage
                    FROM SurveyBucks.Demographics
                    WHERE IsDeleted = 0
                    GROUP BY CASE 
                            WHEN Age < 18 THEN 'Under 18'
                            WHEN Age BETWEEN 18 AND 24 THEN '18-24'
                            WHEN Age BETWEEN 25 AND 34 THEN '25-34'
                            WHEN Age BETWEEN 35 AND 44 THEN '35-44'
                            WHEN Age BETWEEN 45 AND 54 THEN '45-54'
                            WHEN Age BETWEEN 55 AND 64 THEN '55-64'
                            ELSE '65+' 
                         END
                    ORDER BY Value";
                    break;

                case "country":
                    sql = @"
                    SELECT 
                        'Country' AS Category,
                        ISNULL(Country, Location) AS Value,
                        COUNT(*) AS Count,
                        COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SurveyBucks.Demographics WHERE IsDeleted = 0) AS Percentage
                    FROM SurveyBucks.Demographics
                    WHERE IsDeleted = 0
                    GROUP BY ISNULL(Country, Location)
                    ORDER BY Count DESC";
                    break;

                case "education":
                    sql = @"
                    SELECT 
                        'Education' AS Category,
                        HighestEducation AS Value,
                        COUNT(*) AS Count,
                        COUNT(*) * 100.0 / (SELECT COUNT(*) FROM SurveyBucks.Demographics WHERE IsDeleted = 0) AS Percentage
                    FROM SurveyBucks.Demographics
                    WHERE IsDeleted = 0 AND HighestEducation IS NOT NULL
                    GROUP BY HighestEducation
                    ORDER BY Count DESC";
                    break;

                default:
                    return new List<DemographicBreakdownDto>();
            }

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<DemographicBreakdownDto>(sql);
            }
        }

        public async Task<IEnumerable<ActivityTimelineDto>> GetActivityTimelineAsync(int days = 30)
        {
            const string sql = @"
            WITH DateSeries AS (
                SELECT DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date)) AS [Date]
                UNION ALL
                SELECT DATEADD(DAY, 1, [Date])
                FROM DateSeries
                WHERE DATEADD(DAY, 1, [Date]) <= CAST(GETDATE() AS date)
            ),
            UserRegistrations AS (
                SELECT 
                    CAST(RegistrationDate AS date) AS [Date],
                    COUNT(*) AS Registrations
                FROM AspNetUsers
                WHERE RegistrationDate >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date))
                GROUP BY CAST(RegistrationDate AS date)
            ),
            UserLogins AS (
                SELECT 
                    CAST(LastLoginDate AS date) AS [Date],
                    COUNT(*) AS Logins
                FROM AspNetUsers
                WHERE LastLoginDate >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date))
                GROUP BY CAST(LastLoginDate AS date)
            ),
            SurveyStarts AS (
                SELECT 
                    CAST(StartedAtDateTime AS date) AS [Date],
                    COUNT(*) AS SurveyStarts
                FROM SurveyBucks.SurveyParticipation
                WHERE StartedAtDateTime >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date))
                  AND IsDeleted = 0
                GROUP BY CAST(StartedAtDateTime AS date)
            ),
            SurveyCompletions AS (
                SELECT 
                    CAST(CompletedAtDateTime AS date) AS [Date],
                    COUNT(*) AS SurveyCompletions
                FROM SurveyBucks.SurveyParticipation
                WHERE CompletedAtDateTime >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date))
                  AND StatusId IN (3, 7) -- Completed or Rewarded
                  AND IsDeleted = 0
                GROUP BY CAST(CompletedAtDateTime AS date)
            ),
            RewardRedemptions AS (
                SELECT 
                    CAST(ClaimedDate AS date) AS [Date],
                    COUNT(*) AS RewardRedemptions
                FROM SurveyBucks.UserRewards
                WHERE ClaimedDate >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS date))
                  AND RedemptionStatus = 'Claimed'
                  AND IsDeleted = 0
                GROUP BY CAST(ClaimedDate AS date)
            )
            SELECT 
                FORMAT(ds.[Date], 'yyyy-MM-dd') AS [Date],
                ISNULL(ur.Registrations, 0) AS Registrations,
                ISNULL(ul.Logins, 0) AS Logins,
                ISNULL(ss.SurveyStarts, 0) AS SurveyStarts,
                ISNULL(sc.SurveyCompletions, 0) AS SurveyCompletions,
                ISNULL(rr.RewardRedemptions, 0) AS RewardRedemptions
            FROM DateSeries ds
            LEFT JOIN UserRegistrations ur ON ds.[Date] = ur.[Date]
            LEFT JOIN UserLogins ul ON ds.[Date] = ul.[Date]
            LEFT JOIN SurveyStarts ss ON ds.[Date] = ss.[Date]
            LEFT JOIN SurveyCompletions sc ON ds.[Date] = sc.[Date]
            LEFT JOIN RewardRedemptions rr ON ds.[Date] = rr.[Date]
            ORDER BY ds.[Date]
            OPTION (MAXRECURSION 1000)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<ActivityTimelineDto>(sql, new { Days = days });
            }
        }

        public async Task<IEnumerable<ConversionFunnelDto>> GetConversionFunnelAsync(int surveyId)
        {
            const string sql = @"
        SELECT 'Views' AS Stage, sa.TotalViews AS Count, 100 AS ConversionRate
        FROM SurveyBucks.SurveyAnalytics sa
        WHERE sa.SurveyId = @SurveyId
        
        UNION ALL
        
        SELECT 'Enrollments' AS Stage, 
               COUNT(*) AS Count,
               CASE WHEN sa.TotalViews > 0 
                    THEN COUNT(*) * 100.0 / sa.TotalViews 
                    ELSE 0 
               END AS ConversionRate
        FROM SurveyBucks.SurveyParticipation sp
        CROSS JOIN SurveyBucks.SurveyAnalytics sa
        WHERE sp.SurveyId = @SurveyId 
          AND sa.SurveyId = @SurveyId
          AND sp.IsDeleted = 0
        
        UNION ALL
        
        SELECT 'Starts' AS Stage, 
               COUNT(*) AS Count,
               CASE WHEN enrollments.Count > 0 
                    THEN COUNT(*) * 100.0 / enrollments.Count 
                    ELSE 0 
               END AS ConversionRate
        FROM SurveyBucks.SurveyParticipation sp
        CROSS JOIN (
            SELECT COUNT(*) AS Count
            FROM SurveyBucks.SurveyParticipation
            WHERE SurveyId = @SurveyId AND IsDeleted = 0
        ) AS enrollments
        WHERE sp.SurveyId = @SurveyId 
          AND sp.StartedAtDateTime IS NOT NULL
          AND sp.IsDeleted = 0
        
        UNION ALL
        
        SELECT 'Completions' AS Stage, 
               COUNT(*) AS Count,
               CASE WHEN starts.Count > 0 
                    THEN COUNT(*) * 100.0 / starts.Count 
                    ELSE 0 
               END AS ConversionRate
        FROM SurveyBucks.SurveyParticipation sp
        CROSS JOIN (
            SELECT COUNT(*) AS Count
            FROM SurveyBucks.SurveyParticipation
            WHERE SurveyId = @SurveyId 
              AND StartedAtDateTime IS NOT NULL
              AND IsDeleted = 0
        ) AS starts
        WHERE sp.SurveyId = @SurveyId 
          AND sp.StatusId IN (3, 7) -- Completed or Rewarded
          AND sp.IsDeleted = 0
        
        UNION ALL
        
        SELECT 'Rewards' AS Stage, 
               COUNT(*) AS Count,
               CASE WHEN completions.Count > 0 
                    THEN COUNT(*) * 100.0 / completions.Count 
                    ELSE 0 
               END AS ConversionRate
        FROM SurveyBucks.UserRewards ur
        JOIN SurveyBucks.SurveyParticipation sp ON ur.SurveyParticipationId = sp.Id
        CROSS JOIN (
            SELECT COUNT(*) AS Count
            FROM SurveyBucks.SurveyParticipation
            WHERE SurveyId = @SurveyId 
              AND StatusId IN (3, 7) -- Completed or Rewarded
              AND IsDeleted = 0
        ) AS completions
        WHERE sp.SurveyId = @SurveyId 
          AND ur.IsDeleted = 0
          AND sp.IsDeleted = 0
        ORDER BY 
            CASE Stage 
                WHEN 'Views' THEN 1
                WHEN 'Enrollments' THEN 2
                WHEN 'Starts' THEN 3
                WHEN 'Completions' THEN 4
                WHEN 'Rewards' THEN 5
            END";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<ConversionFunnelDto>(sql, new { SurveyId = surveyId });
            }
        }

        public async Task<byte[]> ExportSurveyResponsesAsync(int surveyId, string format = "csv")
        {
            // Get all survey data needed for export
            const string surveyDataSql = @"
        SELECT 
            s.Name AS SurveyName,
            sp.Id AS ParticipationId,
            u.UserName AS Participant,
            q.Text AS QuestionText,
            qt.Name AS QuestionType,
            CASE 
                WHEN qt.Name IN ('SingleChoice', 'MultipleChoice', 'Dropdown') THEN qrc.Text
                ELSE sr.Answer
            END AS Answer,
            sr.ResponseDateTime
        FROM SurveyBucks.SurveyResponse sr
        JOIN SurveyBucks.SurveyParticipation sp ON sr.SurveyParticipationId = sp.Id
        JOIN SurveyBucks.Survey s ON sp.SurveyId = s.Id
        JOIN SurveyBucks.Question q ON sr.QuestionId = q.Id
        JOIN SurveyBucks.QuestionType qt ON q.QuestionTypeId = qt.Id
        JOIN AspNetUsers u ON sp.UserId = u.Id
        LEFT JOIN SurveyBucks.QuestionResponseChoice qrc ON 
            TRY_CAST(sr.Answer AS INT) = qrc.Id
            AND q.Id = qrc.QuestionId
        WHERE sp.SurveyId = @SurveyId
          AND sp.IsDeleted = 0
          AND sr.IsDeleted = 0
        ORDER BY sp.Id, q.Id";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var responses = await connection.QueryAsync<SurveyResponseExportDto>(surveyDataSql, new { SurveyId = surveyId });

                // Now convert to the requested format
                if (format?.ToLower() == "excel")
                {
                    // Create Excel file using a library like EPPlus or ClosedXML
                    // Example with EPPlus (would need to add EPPlus NuGet package)
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Survey Responses");

                        // Add headers
                        worksheet.Cells[1, 1].Value = "Survey";
                        worksheet.Cells[1, 2].Value = "Participant";
                        worksheet.Cells[1, 3].Value = "Question";
                        worksheet.Cells[1, 4].Value = "Answer";
                        worksheet.Cells[1, 5].Value = "Response Date";

                        // Add data
                        int row = 2;
                        foreach (var response in responses)
                        {
                            worksheet.Cells[row, 1].Value = response.SurveyName;
                            worksheet.Cells[row, 2].Value = response.Participant;
                            worksheet.Cells[row, 3].Value = response.QuestionText;
                            worksheet.Cells[row, 4].Value = response.Answer;
                            worksheet.Cells[row, 5].Value = response.ResponseDateTime;
                            row++;
                        }

                        // Format as a table
                        var range = worksheet.Cells[1, 1, row - 1, 5];
                        var table = worksheet.Tables.Add(range, "SurveyResponses");
                        table.ShowHeader = true;

                        worksheet.Cells.AutoFitColumns();

                        return package.GetAsByteArray();
                    }
                }
                else // Default to CSV
                {
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new StreamWriter(memoryStream))
                    {
                        // Write CSV header
                        writer.WriteLine("Survey,Participant,Question,Answer,ResponseDate");

                        // Write data rows
                        foreach (var response in responses)
                        {
                            // Properly escape CSV fields
                            string surveyName = EscapeCsvField(response.SurveyName);
                            string participant = EscapeCsvField(response.Participant);
                            string questionText = EscapeCsvField(response.QuestionText);
                            string answer = EscapeCsvField(response.Answer);
                            string responseDate = response.ResponseDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                            writer.WriteLine($"{surveyName},{participant},{questionText},{answer},{responseDate}");
                        }

                        writer.Flush();
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            bool containsComma = field.Contains(",");
            bool containsQuote = field.Contains("\"");

            if (containsQuote)
                field = field.Replace("\"", "\"\"");

            if (containsComma || containsQuote)
                field = $"\"{field}\"";

            return field;
        }
    }
}
