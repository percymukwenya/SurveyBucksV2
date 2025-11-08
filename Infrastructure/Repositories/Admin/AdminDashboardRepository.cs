using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger<AdminDashboardRepository> _logger;

        public AdminDashboardRepository(IDatabaseConnectionFactory connectionFactory, ILogger<AdminDashboardRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            const string sql = @"
            -- User statistics
            DECLARE @TotalUsers INT = (SELECT COUNT(*) FROM SurveyBucks.Users);
            DECLARE @ActiveUsers INT = (SELECT COUNT(*) FROM SurveyBucks.Users WHERE IsActive = 1 AND LastLoginDate >= DATEADD(DAY, -30, GETDATE()));
            DECLARE @NewUsersToday INT = (SELECT COUNT(*) FROM SurveyBucks.Users WHERE CAST(RegistrationDate AS DATE) = CAST(GETDATE() AS DATE));
            DECLARE @AverageProfileCompletion FLOAT = (
                SELECT AVG(CompletionPercentage) 
                FROM SurveyBucks.DemographicProfileStatus
            );
            DECLARE @PendingVerifications INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Users 
                WHERE EmailConfirmed = 0 AND RegistrationDate >= DATEADD(DAY, -7, GETDATE())
            );

            -- Survey statistics
            DECLARE @TotalSurveys INT = (SELECT COUNT(*) FROM SurveyBucks.Survey WHERE IsDeleted = 0);
            DECLARE @ActiveSurveys INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Survey 
                WHERE IsActive = 1 AND IsPublished = 1 AND IsDeleted = 0
                AND (ClosingDateTime IS NULL OR ClosingDateTime > GETDATE())
            );
            DECLARE @DraftSurveys INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Survey 
                WHERE IsPublished = 0 AND IsDeleted = 0
            );
            DECLARE @PublishedSurveys INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Survey 
                WHERE IsPublished = 1 AND IsDeleted = 0
            );
            DECLARE @CompletedSurveys INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Survey 
                WHERE IsPublished = 1 AND IsDeleted = 0
                AND ClosingDateTime IS NOT NULL AND ClosingDateTime <= GETDATE()
            );
            DECLARE @SurveyCompletions INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.SurveyParticipation 
                WHERE StatusId IN (3, 7) AND IsDeleted = 0
            );
            DECLARE @SurveysWithNewResults INT = (
                SELECT COUNT(DISTINCT SurveyId) 
                FROM SurveyBucks.SurveyParticipation 
                WHERE StatusId IN (3, 7) AND IsDeleted = 0
                AND CompletedAtDateTime >= DATEADD(DAY, -7, GETDATE())
            );

            -- Analytics statistics
            DECLARE @AverageCompletionRate FLOAT = (
                SELECT AVG(CAST(CASE WHEN TotalStarts > 0 THEN (TotalCompletions * 1.0 / TotalStarts) ELSE 0 END AS FLOAT))
                FROM SurveyBucks.SurveyAnalytics
            );
            DECLARE @AverageResponseTimeSeconds INT = (
                SELECT AVG(TimeSpentInSeconds)
                FROM SurveyBucks.ResponseTimeTracking
                WHERE IsDeleted = 0 AND TimeSpentInSeconds > 0
            );

            -- Reward statistics
            DECLARE @RewardsRedeemed INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.UserRewards 
                WHERE RedemptionStatus = 'Claimed' AND IsDeleted = 0
            );
            DECLARE @ActiveRewards INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.Rewards 
                WHERE IsActive = 1 AND IsDeleted = 0
            );
            DECLARE @PendingRedemptions INT = (
                SELECT COUNT(*) 
                FROM SurveyBucks.UserRewards 
                WHERE RedemptionStatus = 'Pending' AND IsDeleted = 0
            );
            DECLARE @PointsIssued INT = (
                SELECT SUM(Amount) 
                FROM SurveyBucks.PointTransactions 
                WHERE TransactionType = 'Earned' AND IsDeleted = 0
            );
            DECLARE @PointsRedeemed INT = (
                SELECT SUM(Amount) 
                FROM SurveyBucks.PointTransactions 
                WHERE TransactionType = 'Redeemed' AND IsDeleted = 0
            );

            -- Return results
            SELECT
                @TotalUsers AS TotalUsers,
                @ActiveUsers AS ActiveUsers,
                @NewUsersToday AS NewUsersToday,
                @AverageProfileCompletion AS AverageProfileCompletion,
                @PendingVerifications AS PendingVerifications,
    
                @TotalSurveys AS TotalSurveys,
                @ActiveSurveys AS ActiveSurveys,
                @DraftSurveys AS DraftSurveys,
                @PublishedSurveys AS PublishedSurveys,
                @CompletedSurveys AS CompletedSurveys,
                @SurveyCompletions AS SurveyCompletions,
                @SurveysWithNewResults AS SurveysWithNewResults,
    
                @AverageCompletionRate AS AverageCompletionRate,
                CASE 
                    WHEN @AverageResponseTimeSeconds < 60 THEN CAST(@AverageResponseTimeSeconds AS VARCHAR) + ' sec'
                    WHEN @AverageResponseTimeSeconds < 3600 THEN CAST(@AverageResponseTimeSeconds/60 AS VARCHAR) + ' min'
                    ELSE CAST(@AverageResponseTimeSeconds/3600 AS VARCHAR) + ' hr'
                END AS AverageResponseTime,
    
                @RewardsRedeemed AS RewardsRedeemed,
                @ActiveRewards AS ActiveRewards,
                @PendingRedemptions AS PendingRedemptions,
                ISNULL(@PointsIssued, 0) AS PointsIssued,
                ISNULL(@PointsRedeemed, 0) AS PointsRedeemed";

            var conn = _connectionFactory.CreateConnection();

            try
            {
                var stats = await conn.QuerySingleAsync<DashboardStatsDto>(sql);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retreiving stats. {ex}");

                throw;
            }
        }
    }
}
