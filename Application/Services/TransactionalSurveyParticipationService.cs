using Dapper;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Response;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TransactionalSurveyParticipationService : ISurveyParticipationService
    {
        private readonly ISurveyParticipationService _baseService;
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ILogger<TransactionalSurveyParticipationService> _logger;

        public TransactionalSurveyParticipationService(
            ISurveyParticipationService baseService,
            IDatabaseConnectionFactory connectionFactory,
            ILogger<TransactionalSurveyParticipationService> logger)
        {
            _baseService = baseService;
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // Delegate most methods to the base service
        public async Task<IEnumerable<SurveyListItemDto>> GetMatchingSurveysForUserAsync(string userId)
            => await _baseService.GetMatchingSurveysForUserAsync(userId);

        public async Task<SurveyAccessResponseDto> GetAvailableSurveysWithAccessCheckAsync(string userId)
            => await _baseService.GetAvailableSurveysWithAccessCheckAsync(userId);

        public async Task<SurveyDetailDto> GetSurveyDetailsAsync(int surveyId, string userId)
            => await _baseService.GetSurveyDetailsAsync(surveyId, userId);

        public async Task<SurveyParticipationDto> GetParticipationAsync(int participationId, string userId)
            => await _baseService.GetParticipationAsync(participationId, userId);

        public async Task<IEnumerable<SurveyParticipationSummaryDto>> GetUserParticipationsAsync(string userId, string status = null)
            => await _baseService.GetUserParticipationsAsync(userId, status);

        public async Task<bool> UpdateParticipationProgressAsync(SurveyProgressUpdateDto progressDto, string userId)
            => await _baseService.UpdateParticipationProgressAsync(progressDto, userId);

        public async Task<bool> SaveSurveyResponseAsync(SurveyResponseDto response, string userId)
            => await _baseService.SaveSurveyResponseAsync(response, userId);

        public async Task<IEnumerable<SurveyResponseDto>> GetSavedResponsesAsync(int participationId, string userId)
            => await _baseService.GetSavedResponsesAsync(participationId, userId);

        public async Task<bool> SubmitSurveyFeedbackAsync(SurveyFeedbackDto feedback, string userId)
            => await _baseService.SubmitSurveyFeedbackAsync(feedback, userId);

        // Override the enrollment method with transaction support
        public async Task<SurveyParticipationDto> EnrollInSurveyAsync(string userId, int surveyId)
        {
            _logger.LogInformation("Starting transactional enrollment for User {UserId} in Survey {SurveyId}", userId, surveyId);

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // 1. BUSINESS VALIDATION within transaction
                var isEligible = await IsUserEligibleForSurveyAsync(connection, transaction, userId, surveyId);
                if (!isEligible.IsEligible)
                {
                    throw new InvalidOperationException(isEligible.Reason);
                }

                // 2. CHECK FOR EXISTING PARTICIPATION
                var existingParticipationId = await GetExistingParticipationAsync(connection, transaction, userId, surveyId);
                if (existingParticipationId.HasValue)
                {
                    _logger.LogInformation("User {UserId} already enrolled in survey {SurveyId}, returning existing participation", userId, surveyId);
                    
                    // Return existing participation (no transaction needed)
                    transaction.Rollback();
                    return await _baseService.GetParticipationAsync(existingParticipationId.Value, userId);
                }

                // 3. CREATE PARTICIPATION RECORD
                var participationId = await CreateParticipationAsync(connection, transaction, userId, surveyId);

                // 4. ADD NOTIFICATION
                await CreateEnrollmentNotificationAsync(connection, transaction, userId, surveyId);

                // 5. UPDATE ANALYTICS
                await UpdateSurveyAnalyticsAsync(connection, transaction, surveyId);

                // 6. LOG ACTIVITY
                //await LogEnrollmentActivityAsync(connection, transaction, userId, surveyId, participationId);

                // Commit transaction if all operations succeed
                transaction.Commit();

                _logger.LogInformation("Successfully enrolled User {UserId} in Survey {SurveyId} with Participation {ParticipationId}", 
                    userId, surveyId, participationId);

                // Return the new participation
                var participation = new SurveyParticipationDto
                {
                    Id = participationId,
                    UserId = userId,
                    SurveyId = surveyId,
                    StatusId = 1, // Enrolled
                    ProgressPercentage = 0,
                    EnrolmentDateTime = DateTime.UtcNow
                };

                // Fire background operations after successful enrollment
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // These operations don't need to be part of the main transaction
                        await FireBackgroundOperationsAsync(userId, surveyId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background operations failed for User {UserId}, Survey {SurveyId}", userId, surveyId);
                    }
                });

                return participation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transactional enrollment for User {UserId}, Survey {SurveyId}", userId, surveyId);
                transaction.Rollback();
                throw;
            }
        }

        // Override completion method with transaction support
        public async Task<bool> CompleteSurveyAsync(int participationId, string userId)
        {
            _logger.LogInformation("Starting transactional completion for User {UserId}, Participation {ParticipationId}", userId, participationId);

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // 1. VALIDATE PARTICIPATION
                var participation = await ValidateParticipationForCompletionAsync(connection, transaction, participationId, userId);
                if (participation == null)
                {
                    throw new InvalidOperationException("Invalid participation or already completed");
                }

                // 2. COMPLETE SURVEY
                var completed = await CompleteSurveyInTransactionAsync(connection, transaction, participationId, userId);
                if (!completed)
                {
                    throw new InvalidOperationException("Failed to complete survey");
                }

                // 3. AWARD POINTS/REWARDS
                await ProcessRewardsAsync(connection, transaction, userId, participation.SurveyId);

                // 4. UPDATE ANALYTICS
                await UpdateCompletionAnalyticsAsync(connection, transaction, participation.SurveyId);

                // 5. CREATE COMPLETION NOTIFICATION
                await CreateCompletionNotificationAsync(connection, transaction, userId, participation.SurveyId);

                transaction.Commit();

                _logger.LogInformation("Successfully completed survey for User {UserId}, Participation {ParticipationId}", userId, participationId);

                // Fire background operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FireCompletionBackgroundOperationsAsync(userId, participation.SurveyId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background completion operations failed for User {UserId}", userId);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transactional completion for User {UserId}, Participation {ParticipationId}", userId, participationId);
                transaction.Rollback();
                throw;
            }
        }

        #region Private Transaction Methods

        private async Task<(bool IsEligible, string Reason)> IsUserEligibleForSurveyAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            const string sql = @"
                -- Check profile completion
                DECLARE @ProfileComplete BIT = 0;
                DECLARE @SurveyActive BIT = 0;
                DECLARE @AlreadyParticipated BIT = 0;

                -- Profile completion check (75% required - 3 of 4 sections)
                DECLARE @SectionCount INT = 0;
                
                -- Demographics section (25%)
                IF EXISTS (
                    SELECT 1 FROM SurveyBucks.Demographics d
                    WHERE d.UserId = @UserId 
                      AND d.Gender IS NOT NULL 
                      AND d.Age > 0 
                      AND d.Income > 0
                      AND d.IsDeleted = 0
                ) SET @SectionCount = @SectionCount + 1;
                
                -- Documents section (25%)
                IF EXISTS (
                    SELECT 1 FROM SurveyBucks.UserDocument ud
                    JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                    WHERE ud.UserId = @UserId 
                      AND dt.Category = 'Identity'
                      AND ud.VerificationStatus = 'Approved'
                      AND ud.IsDeleted = 0
                ) SET @SectionCount = @SectionCount + 1;
                
                -- Banking section (25%)
                IF EXISTS (
                    SELECT 1 FROM SurveyBucks.BankingDetail bd
                    WHERE bd.UserId = @UserId 
                      AND bd.IsVerified = 1 
                      AND bd.IsActive = 1
                      AND bd.IsDeleted = 0
                ) SET @SectionCount = @SectionCount + 1;
                
                -- Interests section (25%)
                IF (
                    SELECT COUNT(*) FROM SurveyBucks.UserInterests 
                    WHERE UserId = @UserId
                ) >= 3 SET @SectionCount = @SectionCount + 1;
                
                -- 75% = 3 out of 4 sections complete
                SELECT @ProfileComplete = CASE WHEN @SectionCount >= 3 THEN 1 ELSE 0 END;

                -- Survey active check
                SELECT @SurveyActive = CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM SurveyBucks.Survey s
                        WHERE s.Id = @SurveyId 
                          AND s.IsActive = 1 
                          AND s.OpeningDateTime <= GETDATE() 
                          AND s.ClosingDateTime > GETDATE()
                          AND s.IsDeleted = 0
                    ) THEN 1 ELSE 0 END;

                -- Already participated check
                SELECT @AlreadyParticipated = CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM SurveyBucks.SurveyParticipation sp
                        WHERE sp.UserId = @UserId 
                          AND sp.SurveyId = @SurveyId 
                          AND sp.IsDeleted = 0
                    ) THEN 1 ELSE 0 END;

                SELECT @ProfileComplete as ProfileComplete, 
                       @SurveyActive as SurveyActive, 
                       @AlreadyParticipated as AlreadyParticipated";

            var result = await connection.QuerySingleAsync(sql, new { UserId = userId, SurveyId = surveyId }, transaction);

            if (result.ProfileComplete == false)
                return (false, "Profile must be 75% complete to enroll in surveys");
            if (result.SurveyActive == false)
                return (false, "This survey is not currently active");
            if (result.AlreadyParticipated == true)
                return (false, "Already enrolled in this survey");

            return (true, "Eligible");
        }

        private async Task<int?> GetExistingParticipationAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            const string sql = @"
                SELECT Id FROM SurveyBucks.SurveyParticipation 
                WHERE UserId = @UserId AND SurveyId = @SurveyId AND IsDeleted = 0";

            return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { UserId = userId, SurveyId = surveyId }, transaction);
        }

        private async Task<int> CreateParticipationAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.SurveyParticipation (
                    EnrolmentDateTime, UserId, SurveyId, StatusId,
                    ProgressPercentage, CreatedDate, CreatedBy
                ) VALUES (
                    @EnrolmentDateTime, @UserId, @SurveyId, 1,
                    0, @CreatedDate, @UserId
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var now = DateTime.UtcNow;
            return await connection.QuerySingleAsync<int>(sql, new
            {
                EnrolmentDateTime = now,
                UserId = userId,
                SurveyId = surveyId,
                CreatedDate = now
            }, transaction);
        }

        private async Task CreateEnrollmentNotificationAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.UserNotification (
                    UserId, NotificationTypeId, Title, Message, 
                    ReferenceId, ReferenceType, CreatedDate, IsRead
                )
                SELECT 
                    @UserId, nt.Id, 'Survey Enrollment', 
                    'You have successfully enrolled in: ' + s.Name,
                    CONVERT(NVARCHAR(50), @SurveyId), 'Survey', @CreatedDate, 0
                FROM SurveyBucks.NotificationType nt
                CROSS JOIN SurveyBucks.Survey s
                WHERE nt.Name = 'SurveyInvitation' AND s.Id = @SurveyId";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                SurveyId = surveyId,
                CreatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task UpdateSurveyAnalyticsAsync(
            IDbConnection connection, IDbTransaction transaction, int surveyId)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM SurveyBucks.SurveyAnalytics WHERE SurveyId = @SurveyId)
                BEGIN
                    UPDATE SurveyBucks.SurveyAnalytics
                    SET TotalEnrollments = ISNULL(TotalEnrollments, 0) + 1,
                        LastUpdated = @UpdatedDate
                    WHERE SurveyId = @SurveyId;
                END
                ELSE
                BEGIN
                    INSERT INTO SurveyBucks.SurveyAnalytics (
                        SurveyId, TotalEnrollments, LastUpdated
                    ) VALUES (
                        @SurveyId, 1, @UpdatedDate
                    );
                END";

            await connection.ExecuteAsync(sql, new
            {
                SurveyId = surveyId,
                UpdatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task LogEnrollmentActivityAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId, int participationId)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.ActivityLog (
                    UserId, ActivityType, Description, ReferenceId, 
                    ReferenceType, CreatedDate
                ) VALUES (
                    @UserId, 'SurveyEnrollment', 
                    'User enrolled in survey: ' + CAST(@SurveyId AS NVARCHAR(10)),
                    @ParticipationId, 'Participation', @CreatedDate
                )";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                SurveyId = surveyId,
                ParticipationId = participationId,
                CreatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task<SurveyParticipationDto> ValidateParticipationForCompletionAsync(
            IDbConnection connection, IDbTransaction transaction, int participationId, string userId)
        {
            const string sql = @"
                SELECT Id, UserId, SurveyId, StatusId, ProgressPercentage
                FROM SurveyBucks.SurveyParticipation 
                WHERE Id = @ParticipationId 
                  AND UserId = @UserId 
                  AND StatusId NOT IN (3, 4) -- Not completed or rewarded
                  AND IsDeleted = 0";

            return await connection.QuerySingleOrDefaultAsync<SurveyParticipationDto>(sql, new
            {
                ParticipationId = participationId,
                UserId = userId
            }, transaction);
        }

        private async Task<bool> CompleteSurveyInTransactionAsync(
            IDbConnection connection, IDbTransaction transaction, int participationId, string userId)
        {
            const string sql = @"
                UPDATE SurveyBucks.SurveyParticipation 
                SET StatusId = 3, -- Completed
                    CompletionDateTime = @CompletionDateTime,
                    ProgressPercentage = 100,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @UserId
                WHERE Id = @ParticipationId 
                  AND UserId = @UserId
                  AND IsDeleted = 0";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                ParticipationId = participationId,
                UserId = userId,
                CompletionDateTime = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }, transaction);

            return affectedRows > 0;
        }

        private async Task ProcessRewardsAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            // This would integrate with your rewards system
            const string sql = @"
                -- Award points based on survey completion
                INSERT INTO SurveyBucks.UserPoints (
                    UserId, Points, Source, ReferenceId, 
                    ReferenceType, CreatedDate, Description
                )
                SELECT 
                    @UserId, ISNULL(s.RewardPoints, 100), 'SurveyCompletion', 
                    @SurveyId, 'Survey', @CreatedDate,
                    'Completed survey: ' + s.Name
                FROM SurveyBucks.Survey s
                WHERE s.Id = @SurveyId";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                SurveyId = surveyId,
                CreatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task UpdateCompletionAnalyticsAsync(
            IDbConnection connection, IDbTransaction transaction, int surveyId)
        {
            const string sql = @"
                UPDATE SurveyBucks.SurveyAnalytics
                SET TotalCompletions = ISNULL(TotalCompletions, 0) + 1,
                    LastUpdated = @UpdatedDate
                WHERE SurveyId = @SurveyId";

            await connection.ExecuteAsync(sql, new
            {
                SurveyId = surveyId,
                UpdatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task CreateCompletionNotificationAsync(
            IDbConnection connection, IDbTransaction transaction, string userId, int surveyId)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.UserNotification (
                    UserId, NotificationTypeId, Title, Message, 
                    ReferenceId, ReferenceType, CreatedDate, IsRead
                )
                SELECT 
                    @UserId, nt.Id, 'Survey Completed', 
                    'Congratulations! You completed: ' + s.Name + '. Your reward has been processed.',
                    CONVERT(NVARCHAR(50), @SurveyId), 'Survey', @CreatedDate, 0
                FROM SurveyBucks.NotificationType nt
                CROSS JOIN SurveyBucks.Survey s
                WHERE nt.Name = 'SurveyCompletion' AND s.Id = @SurveyId";

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                SurveyId = surveyId,
                CreatedDate = DateTime.UtcNow
            }, transaction);
        }

        private async Task FireBackgroundOperationsAsync(string userId, int surveyId)
        {
            // These can be safely executed outside the main transaction
            // Gamification, emails, etc.
            await Task.Delay(100); // Simulate background work
            _logger.LogDebug("Background enrollment operations completed for User {UserId}, Survey {SurveyId}", userId, surveyId);
        }

        private async Task FireCompletionBackgroundOperationsAsync(string userId, int surveyId)
        {
            // These can be safely executed outside the main transaction
            await Task.Delay(100); // Simulate background work
            _logger.LogDebug("Background completion operations completed for User {UserId}, Survey {SurveyId}", userId, surveyId);
        }

        #endregion
    }
}