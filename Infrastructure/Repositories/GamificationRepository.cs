using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Infrastructure.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class GamificationRepository : IGamificationRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public GamificationRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<LeaderboardSummaryDto>> GetAvailableLeaderboardsAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    l.Id, l.Name, l.Description, l.LeaderboardType, l.TimePeriod,
                    l.StartDate, l.EndDate, l.IsActive, l.RewardPoints,
                    (SELECT COUNT(*) FROM SurveyBucks.LeaderboardEntries WHERE LeaderboardId = l.Id) AS EntryCount,
                    (SELECT MAX(le.Rank) 
                     FROM SurveyBucks.LeaderboardEntries le 
                     WHERE le.LeaderboardId = l.Id AND le.UserId = @UserId) AS UserRank
                FROM SurveyBucks.Leaderboards l
                WHERE l.IsActive = 1
                  AND l.IsDeleted = 0
                  AND (l.StartDate IS NULL OR l.StartDate <= SYSDATETIMEOFFSET())
                  AND (l.EndDate IS NULL OR l.EndDate >= SYSDATETIMEOFFSET())
                ORDER BY l.TimePeriod, l.Name";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<LeaderboardSummaryDto>(sql, new { UserId = userId });
            }
        }

        public async Task<UserEngagementDto> GetUserEngagementAsync(string userId)
        {
            const string sql = @"
            SELECT ue.Id, ue.UserId, ue.LastLoginDate, ue.CurrentLoginStreak, 
                   ue.MaxLoginStreak, ue.TotalLogins, ue.TotalSurveysCompleted,
                   ue.TotalSurveysStarted, ue.CompletionRate, ue.TotalPointsEarned,
                   ue.TotalRewardsRedeemed, ue.LastActivityDate
            FROM SurveyBucks.UserEngagement ue
            WHERE ue.UserId = @UserId AND ue.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var engagement = await connection.QuerySingleOrDefaultAsync<UserEngagementDto>(sql, new { UserId = userId });

                if (engagement == null)
                {
                    // Create default engagement record if none exists
                    const string insertSql = @"
                    INSERT INTO SurveyBucks.UserEngagement (
                        UserId, CurrentLoginStreak, MaxLoginStreak, TotalLogins,
                        TotalSurveysCompleted, TotalSurveysStarted, TotalPointsEarned,
                        TotalRewardsRedeemed, LastActivityDate, CreatedDate
                    ) VALUES (
                        @UserId, 0, 0, 0, 0, 0, 0, 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                    );
                    
                    SELECT SCOPE_IDENTITY() AS Id, @UserId AS UserId, NULL AS LastLoginDate, 
                           0 AS CurrentLoginStreak, 0 AS MaxLoginStreak, 0 AS TotalLogins,
                           0 AS TotalSurveysCompleted, 0 AS TotalSurveysStarted, 0 AS CompletionRate,
                           0 AS TotalPointsEarned, 0 AS TotalRewardsRedeemed, SYSDATETIMEOFFSET() AS LastActivityDate";

                    engagement = await connection.QuerySingleAsync<UserEngagementDto>(insertSql, new { UserId = userId });
                }

                return engagement;
            }
        }

        public async Task<bool> UpdateLoginStreakAsync(string userId)
        {
            // Use the stored procedure for updating login streak
            using var connection = _connectionFactory.CreateConnection();

            var result = await connection.ExecuteAsync(
                "SurveyBucks.up_UpdateUserLoginStreak",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);

            return result > 0;
        }

        public async Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(string userId)
        {
            const string sql = @"
            SELECT ua.Id, ua.UserId, ua.AchievementId, a.Name AS AchievementName,
                   a.Description AS AchievementDescription, a.Category, a.ImageUrl,
                   ua.PointsAwarded, ua.EarnedDate, ua.EarnedCount, ua.LastEarnedDate
            FROM SurveyBucks.UserAchievements ua
            JOIN SurveyBucks.Achievements a ON ua.AchievementId = a.Id
            WHERE ua.UserId = @UserId AND ua.IsDeleted = 0
            ORDER BY ua.LastEarnedDate DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<UserAchievementDto>(sql, new { UserId = userId });
            }
        }

        public async Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId)
        {
            const string sql = @"
            -- Get all active challenges
            SELECT c.Id, c.Name, c.Description, c.StartDate, c.EndDate,
                   c.RequiredActionType, c.RequiredActionCount, c.PointsAwarded,
                   c.RewardId, c.ImageUrl, c.IsActive,
                   -- Get user's progress if they've started the challenge
                   uc.Progress, uc.IsCompleted, uc.CompletedDate
            FROM SurveyBucks.Challenges c
            LEFT JOIN SurveyBucks.UserChallenges uc ON c.Id = uc.ChallengeId AND uc.UserId = @UserId AND uc.IsDeleted = 0
            WHERE c.IsActive = 1 
              AND c.IsDeleted = 0
              AND GETDATE() BETWEEN c.StartDate AND c.EndDate
            ORDER BY c.EndDate ASC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<ChallengeDto>(sql, new { UserId = userId });
            }
        }

        public async Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                // First get the leaderboard info
                const string leaderboardSql = @"
                SELECT Id, Name, Description, LeaderboardType, TimePeriod
                FROM SurveyBucks.Leaderboards
                WHERE Id = @LeaderboardId AND IsActive = 1 AND IsDeleted = 0";

                var leaderboard = await connection.QuerySingleOrDefaultAsync<LeaderboardDto>(
                    leaderboardSql, new { LeaderboardId = leaderboardId });

                if (leaderboard == null)
                {
                    return null;
                }

                // Get top entries
                const string entriesSql = @"
                SELECT le.Id, le.LeaderboardId, le.UserId, u.UserName as Username,
                       le.Score, le.Rank, le.PreviousRank
                FROM SurveyBucks.LeaderboardEntries le
                JOIN AspNetUsers u ON le.UserId = u.Id
                WHERE le.LeaderboardId = @LeaderboardId AND le.IsDeleted = 0
                ORDER BY le.Rank
                OFFSET 0 ROWS FETCH NEXT @Top ROWS ONLY";

                leaderboard.Entries = (await connection.QueryAsync<LeaderboardEntryDto>(
                    entriesSql, new { LeaderboardId = leaderboardId, Top = top })).ToList();

                // Get current user's rank
                const string userRankSql = @"
                SELECT le.Rank, le.Score
                FROM SurveyBucks.LeaderboardEntries le
                WHERE le.LeaderboardId = @LeaderboardId AND le.UserId = @UserId AND le.IsDeleted = 0";

                var userRank = await connection.QuerySingleOrDefaultAsync<(int Rank, int Score)>(
                    userRankSql, new { LeaderboardId = leaderboardId, UserId = userId });

                leaderboard.UserRank = userRank.Rank;
                leaderboard.UserScore = userRank.Score;

                return leaderboard;
            }
        }

        public async Task<UserLevelDto> GetUserLevelAsync(string userId)
        {
            const string sql = @"
            -- Get user's current level info
            SELECT ul.Level, ul.Name, ul.Description, ul.PointsRequired,
                   ul.ImageUrl, ul.PointsMultiplier, ul.UnlocksRewardCategories,
                   up.TotalPoints AS CurrentUserPoints,
                   ISNULL(nextLevel.PointsRequired - up.TotalPoints, 0) AS PointsToNextLevel,
                   CASE 
                       WHEN nextLevel.Level IS NULL THEN 100
                       ELSE CAST(((up.TotalPoints - ul.PointsRequired) * 100.0 / 
                               (nextLevel.PointsRequired - ul.PointsRequired)) AS INT)
                   END AS ProgressPercentage
            FROM SurveyBucks.UserPoints up
            JOIN SurveyBucks.UserLevels ul ON up.PointsLevel = ul.Level
            LEFT JOIN SurveyBucks.UserLevels nextLevel ON nextLevel.Level = ul.Level + 1
            WHERE up.UserId = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var userLevel = await connection.QuerySingleOrDefaultAsync<UserLevelDto>(sql, new { UserId = userId });

                if (userLevel == null)
                {
                    // If user has no points record, get the base level info
                    const string baseLevelSql = @"
                    SELECT Level, Name, Description, PointsRequired,
                           ImageUrl, PointsMultiplier, UnlocksRewardCategories,
                           0 AS CurrentUserPoints,
                           PointsRequired AS PointsToNextLevel,
                           0 AS ProgressPercentage
                    FROM SurveyBucks.UserLevels
                    WHERE Level = 1";

                    userLevel = await connection.QuerySingleAsync<UserLevelDto>(baseLevelSql);
                }

                return userLevel;
            }
        }

        public async Task<bool> CheckForAchievementsAsync(string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get user stats
                        const string statsSql = @"
                        SELECT ue.TotalSurveysCompleted, ue.CurrentLoginStreak,
                               up.TotalPoints
                        FROM SurveyBucks.UserEngagement ue
                        LEFT JOIN SurveyBucks.UserPoints up ON ue.UserId = up.UserId
                        WHERE ue.UserId = @UserId";

                        var stats = await connection.QuerySingleOrDefaultAsync<(int TotalSurveys, int LoginStreak, int TotalPoints)>(
                            statsSql, new { UserId = userId }, transaction);

                        if (stats == default)
                        {
                            return false;
                        }

                        // Check for eligible achievements
                        const string achievementsSql = @"
                        -- Get achievements that the user could qualify for
                        SELECT a.Id, a.Name, a.Description, a.Category, a.ImageUrl,
                               a.PointsAwarded, a.RequiredActionType, a.RequiredActionCount,
                               a.IsRepeatable, a.RepeatCooldownDays,
                               ua.Id AS UserAchievementId, ua.EarnedCount, ua.LastEarnedDate
                        FROM SurveyBucks.Achievements a
                        LEFT JOIN SurveyBucks.UserAchievements ua ON a.Id = ua.AchievementId 
                                                                   AND ua.UserId = @UserId
                                                                   AND ua.IsDeleted = 0
                        WHERE a.IsActive = 1 AND a.IsDeleted = 0
                          AND (
                              -- Never earned OR repeatable
                              ua.Id IS NULL OR a.IsRepeatable = 1
                          )
                          -- Filter for within cooldown period if applicable
                          AND (
                              ua.LastEarnedDate IS NULL OR a.RepeatCooldownDays IS NULL OR
                              DATEADD(DAY, a.RepeatCooldownDays, ua.LastEarnedDate) < GETDATE()
                          )";

                        var achievements = await connection.QueryAsync<(
                            int Id, string Name, string Description, string Category, string ImageUrl,
                            int PointsAwarded, string RequiredActionType, int RequiredActionCount,
                            bool IsRepeatable, int? RepeatCooldownDays,
                            int? UserAchievementId, int? EarnedCount, DateTime? LastEarnedDate
                        )>(achievementsSql, new { UserId = userId }, transaction);

                        bool achievementsEarned = false;

                        foreach (var achievement in achievements)
                        {
                            bool qualified = false;

                            // Check if user qualifies for this achievement
                            switch (achievement.RequiredActionType)
                            {
                                case "CompleteSurveys":
                                    qualified = stats.TotalSurveys >= achievement.RequiredActionCount;
                                    break;

                                case "ConsecutiveLogins":
                                    qualified = stats.LoginStreak >= achievement.RequiredActionCount;
                                    break;

                                case "EarnPoints":
                                    qualified = stats.TotalPoints >= achievement.RequiredActionCount;
                                    break;

                                    // Add more achievement types as needed
                            }

                            if (qualified)
                            {
                                achievementsEarned = true;

                                if (achievement.UserAchievementId.HasValue)
                                {
                                    // Update existing achievement
                                    const string updateSql = @"
                                    UPDATE SurveyBucks.UserAchievements
                                    SET EarnedCount = EarnedCount + 1,
                                        LastEarnedDate = SYSDATETIMEOFFSET(),
                                        IsNotified = 0,
                                        ModifiedDate = SYSDATETIMEOFFSET(),
                                        ModifiedBy = 'system'
                                    WHERE Id = @UserAchievementId";

                                    await connection.ExecuteAsync(updateSql,
                                        new { UserAchievementId = achievement.UserAchievementId },
                                        transaction);
                                }
                                else
                                {
                                    // Insert new achievement
                                    const string insertSql = @"
                                    INSERT INTO SurveyBucks.UserAchievements (
                                        UserId, AchievementId, EarnedDate, PointsAwarded,
                                        EarnedCount, LastEarnedDate, IsNotified,
                                        CreatedDate, CreatedBy
                                    ) VALUES (
                                        @UserId, @AchievementId, SYSDATETIMEOFFSET(), @PointsAwarded,
                                        1, SYSDATETIMEOFFSET(), 0,
                                        SYSDATETIMEOFFSET(), 'system'
                                    )";

                                    await connection.ExecuteAsync(insertSql,
                                        new
                                        {
                                            UserId = userId,
                                            AchievementId = achievement.Id,
                                            PointsAwarded = achievement.PointsAwarded
                                        },
                                        transaction);
                                }

                                // Add notification
                                const string notificationSql = @"
                                INSERT INTO SurveyBucks.UserNotification (
                                    UserId, NotificationTypeId, Title, Message,
                                    ReferenceId, ReferenceType, CreatedDate
                                )
                                SELECT 
                                    @UserId, Id, 'Achievement Unlocked!', 
                                    @Message,
                                    @ReferenceId, 'Achievement', SYSDATETIMEOFFSET()
                                FROM SurveyBucks.NotificationType
                                WHERE Name = 'AchievementEarned'";

                                await connection.ExecuteAsync(notificationSql,
                                    new
                                    {
                                        UserId = userId,
                                        Message = $"You've earned the {achievement.Name} achievement!",
                                        ReferenceId = achievement.Id.ToString()
                                    },
                                    transaction);

                                // Award points
                                if (achievement.PointsAwarded > 0)
                                {
                                    await connection.ExecuteAsync(
                                        "SurveyBucks.sp_AwardPointsForAction",
                                        new
                                        {
                                            UserId = userId,
                                            ActionType = "AchievementUnlock",
                                            ActionCount = 1,
                                            ReferenceId = achievement.Id.ToString()
                                        },
                                        commandType: CommandType.StoredProcedure,
                                        transaction: transaction);
                                }
                            }
                        }

                        // Update challenges progress
                        await UpdateUserChallengesAsync(connection, transaction, userId);

                        transaction.Commit();
                        return achievementsEarned;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateUserChallengesAsync(IDbConnection connection, IDbTransaction transaction, string userId)
        {
            // Get active challenges for user
            const string activeChallengesSql = @"
            SELECT c.Id, c.RequiredActionType, c.RequiredActionCount,
                   c.PointsAwarded, c.RewardId,
                   uc.Id AS UserChallengeId, uc.Progress, uc.IsCompleted
            FROM SurveyBucks.Challenges c
            LEFT JOIN SurveyBucks.UserChallenges uc ON c.Id = uc.ChallengeId 
                                                    AND uc.UserId = @UserId
                                                    AND uc.IsDeleted = 0
            WHERE c.IsActive = 1 
              AND c.IsDeleted = 0
              AND GETDATE() BETWEEN c.StartDate AND c.EndDate
              AND (uc.IsCompleted IS NULL OR uc.IsCompleted = 0)";

            var challenges = await connection.QueryAsync<(
                int Id, string RequiredActionType, int RequiredActionCount,
                int PointsAwarded, int? RewardId,
                int? UserChallengeId, int? Progress, bool? IsCompleted
            )>(activeChallengesSql, new { UserId = userId }, transaction);

            if (!challenges.Any())
            {
                return false;
            }

            // Get user stats for challenge evaluation
            const string statsSql = @"
            SELECT ue.TotalSurveysCompleted, ue.TotalSurveysStarted,
                   ue.CurrentLoginStreak, ue.TotalLogins,
                   up.TotalPoints, up.RedeemedPoints
            FROM SurveyBucks.UserEngagement ue
            LEFT JOIN SurveyBucks.UserPoints up ON ue.UserId = up.UserId
            WHERE ue.UserId = @UserId";

            var stats = await connection.QuerySingleAsync<(
                int TotalSurveysCompleted, int TotalSurveysStarted,
                int CurrentLoginStreak, int TotalLogins,
                int TotalPoints, int RedeemedPoints
            )>(statsSql, new { UserId = userId }, transaction);

            bool challengesUpdated = false;

            foreach (var challenge in challenges)
            {
                int currentProgress = 0;

                // Calculate progress based on challenge type
                switch (challenge.RequiredActionType)
                {
                    case "CompleteSurveys":
                        currentProgress = stats.TotalSurveysCompleted;
                        break;

                    case "ConsecutiveLogins":
                        currentProgress = stats.CurrentLoginStreak;
                        break;

                    case "TotalLogins":
                        currentProgress = stats.TotalLogins;
                        break;

                    case "EarnPoints":
                        currentProgress = stats.TotalPoints;
                        break;

                        // Add more challenge types as needed
                }

                // Limit progress to required count
                currentProgress = Math.Min(currentProgress, challenge.RequiredActionCount);

                // Determine if challenge is now completed
                bool isCompleted = currentProgress >= challenge.RequiredActionCount;

                if (challenge.UserChallengeId.HasValue)
                {
                    // Update existing user challenge
                    if (currentProgress > challenge.Progress || isCompleted != challenge.IsCompleted)
                    {
                        const string updateSql = @"
                        UPDATE SurveyBucks.UserChallenges
                        SET Progress = @Progress,
                            IsCompleted = @IsCompleted,
                            CompletedDate = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                             THEN SYSDATETIMEOFFSET() ELSE CompletedDate END,
                            PointsAwarded = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                              THEN @PointsAwarded ELSE PointsAwarded END,
                            IsRewarded = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                           THEN 1 ELSE IsRewarded END,
                            ModifiedDate = SYSDATETIMEOFFSET()
                        WHERE Id = @UserChallengeId";

                        await connection.ExecuteAsync(updateSql,
                            new
                            {
                                UserChallengeId = challenge.UserChallengeId,
                                Progress = currentProgress,
                                IsCompleted = isCompleted,
                                PointsAwarded = challenge.PointsAwarded
                            },
                            transaction);

                        challengesUpdated = true;

                        // If newly completed, add notification and award points/rewards
                        if (isCompleted && challenge.IsCompleted != true)
                        {
                            await ProcessChallengeCompletionAsync(connection, transaction, userId, challenge.Id, challenge.PointsAwarded, challenge.RewardId);
                        }
                    }
                }
                else
                {
                    // Insert new user challenge
                    const string insertSql = @"
                    INSERT INTO SurveyBucks.UserChallenges (
                        UserId, ChallengeId, Progress, IsCompleted,
                        CompletedDate, PointsAwarded, IsRewarded,
                        CreatedDate
                    ) VALUES (
                        @UserId, @ChallengeId, @Progress, @IsCompleted,
                        CASE WHEN @IsCompleted = 1 THEN SYSDATETIMEOFFSET() ELSE NULL END,
                        CASE WHEN @IsCompleted = 1 THEN @PointsAwarded ELSE NULL END,
                        CASE WHEN @IsCompleted = 1 THEN 1 ELSE 0 END,
                        SYSDATETIMEOFFSET()
                    )";

                    await connection.ExecuteAsync(insertSql,
                        new
                        {
                            UserId = userId,
                            ChallengeId = challenge.Id,
                            Progress = currentProgress,
                            IsCompleted = isCompleted,
                            PointsAwarded = challenge.PointsAwarded
                        },
                        transaction);

                    challengesUpdated = true;

                    // If completed on creation, add notification and award points/rewards
                    if (isCompleted)
                    {
                        await ProcessChallengeCompletionAsync(connection, transaction, userId, challenge.Id, challenge.PointsAwarded, challenge.RewardId);
                    }
                }
            }

            return challengesUpdated;
        }

        private async Task ProcessChallengeCompletionAsync(IDbConnection connection, IDbTransaction transaction, string userId, int challengeId, int pointsAwarded, int? rewardId)
        {
            // Get challenge info for notification
            const string challengeSql = @"
            SELECT Name, Description
            FROM SurveyBucks.Challenges
            WHERE Id = @ChallengeId";

            var challenge = await connection.QuerySingleAsync<(string Name, string Description)>(
                challengeSql, new { ChallengeId = challengeId }, transaction);

            // Add notification
            const string notificationSql = @"
            INSERT INTO SurveyBucks.UserNotification (
                UserId, NotificationTypeId, Title, Message,
                ReferenceId, ReferenceType, CreatedDate
            )
            SELECT 
                @UserId, Id, 'Challenge Completed!', 
                @Message,
                @ReferenceId, 'Challenge', SYSDATETIMEOFFSET()
            FROM SurveyBucks.NotificationType
            WHERE Name = 'AchievementEarned'"; // Reuse achievement notification type

            await connection.ExecuteAsync(notificationSql,
                new
                {
                    UserId = userId,
                    Message = $"You've completed the {challenge.Name} challenge!",
                    ReferenceId = challengeId.ToString()
                },
                transaction);

            // Award points
            if (pointsAwarded > 0)
            {
                await connection.ExecuteAsync(
                    "SurveyBucks.sp_AwardPointsForAction",
                    new
                    {
                        UserId = userId,
                        ActionType = "ChallengeCompletion",
                        ActionCount = 1,
                        ReferenceId = challengeId.ToString()
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction);
            }

            // Award reward if applicable
            if (rewardId.HasValue)
            {
                const string rewardSql = @"
                INSERT INTO SurveyBucks.UserRewards (
                    UserId, RewardId, SurveyParticipationId, EarnedDate, 
                    RedemptionStatus, RedemptionCode, DeliveryStatus, 
                    CreatedDate, CreatedBy
                )
                VALUES (
                    @UserId, @RewardId, 
                    NULL, -- Challenge reward, not survey-specific
                    SYSDATETIMEOFFSET(), 'Unclaimed',
                    'CHG-' + UPPER(LEFT(CONVERT(NVARCHAR(50), NEWID()), 8)),
                    'Pending', SYSDATETIMEOFFSET(), 'system'
                )";

                await connection.ExecuteAsync(rewardSql,
                    new { UserId = userId, RewardId = rewardId },
                    transaction);

                // Add reward notification
                const string rewardNotificationSql = @"
                INSERT INTO SurveyBucks.UserNotification (
                    UserId, NotificationTypeId, Title, Message,
                    ReferenceId, ReferenceType, CreatedDate
                )
                SELECT 
                    @UserId, Id, 'Challenge Reward Earned', 
                    @Message,
                    @ReferenceId, 'Reward', SYSDATETIMEOFFSET()
                FROM SurveyBucks.NotificationType
                WHERE Name = 'RewardEarned'";

                await connection.ExecuteAsync(rewardNotificationSql,
                    new
                    {
                        UserId = userId,
                        Message = $"You've earned a reward for completing the {challenge.Name} challenge!",
                        ReferenceId = rewardId.ToString()
                    },
                    transaction);
            }
        }

        public async Task<bool> UpdateLeaderboardsAsync()
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get active leaderboards
                        const string leaderboardsSql = @"
                        SELECT Id, LeaderboardType, TimePeriod
                        FROM SurveyBucks.Leaderboards
                        WHERE IsActive = 1 AND IsDeleted = 0";

                        var leaderboards = await connection.QueryAsync<(int Id, string LeaderboardType, string TimePeriod)>(
                            leaderboardsSql, null, transaction);

                        foreach (var leaderboard in leaderboards)
                        {
                            // Determine date range based on time period
                            string dateRangeSql = "";
                            switch (leaderboard.TimePeriod)
                            {
                                case "Daily":
                                    dateRangeSql = "AND CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE)";
                                    break;

                                case "Weekly":
                                    dateRangeSql = "AND CreatedDate >= DATEADD(DAY, -7, GETDATE())";
                                    break;

                                case "Monthly":
                                    dateRangeSql = "AND CreatedDate >= DATEADD(MONTH, -1, GETDATE())";
                                    break;

                                    // AllTime has no date filter
                            }

                            // Get user scores based on leaderboard type
                            string scoreSourceSql = "";
                            switch (leaderboard.LeaderboardType)
                            {
                                case "Points":
                                    scoreSourceSql = $@"
                                    SELECT UserId, SUM(Amount) AS Score
                                    FROM SurveyBucks.PointTransactions
                                    WHERE TransactionType = 'Earned' AND IsDeleted = 0
                                    {dateRangeSql}
                                    GROUP BY UserId
                                    HAVING SUM(Amount) > 0";
                                    break;

                                case "Surveys":
                                    scoreSourceSql = $@"
                                    SELECT UserId, COUNT(*) AS Score
                                    FROM SurveyBucks.SurveyParticipation
                                    WHERE StatusId IN (3, 7) AND IsDeleted = 0 -- Completed or Rewarded
                                    {dateRangeSql}
                                    GROUP BY UserId
                                    HAVING COUNT(*) > 0";
                                    break;

                                case "Streak":
                                    scoreSourceSql = $@"
                                    SELECT UserId, CurrentLoginStreak AS Score
                                    FROM SurveyBucks.UserEngagement
                                    WHERE CurrentLoginStreak > 0 AND IsDeleted = 0";
                                    break;

                                    // Add more leaderboard types as needed
                            }

                            if (string.IsNullOrEmpty(scoreSourceSql))
                            {
                                continue;
                            }

                            // Create a temp table for the new rankings
                            await connection.ExecuteAsync(
                                "CREATE TABLE #TempRankings (UserId NVARCHAR(255), Score INT, Rank INT)",
                                null, transaction);

                            // Insert scores with ranking
                            string rankingSql = $@"
                            INSERT INTO #TempRankings (UserId, Score, Rank)
                            SELECT UserId, Score, 
                                   DENSE_RANK() OVER (ORDER BY Score DESC) AS Rank
                            FROM ({scoreSourceSql}) AS Scores";

                            await connection.ExecuteAsync(rankingSql, null, transaction);

                            // Save current entries for tracking rank changes
                            await connection.ExecuteAsync(
                                "CREATE TABLE #OldRankings (UserId NVARCHAR(255), Rank INT)",
                                null, transaction);

                            await connection.ExecuteAsync(@"
                            INSERT INTO #OldRankings (UserId, Rank)
                            SELECT UserId, Rank
                            FROM SurveyBucks.LeaderboardEntries
                            WHERE LeaderboardId = @LeaderboardId AND IsDeleted = 0",
                                new { LeaderboardId = leaderboard.Id }, transaction);

                            // Clear existing entries                           
                            await connection.ExecuteAsync(@"    
                                DELETE FROM SurveyBucks.LeaderboardEntries    
                                WHERE LeaderboardId = @LeaderboardId",
                                new { LeaderboardId = leaderboard.Id }, transaction);

                            // Insert new entries with previous rank
                            await connection.ExecuteAsync(@"
                            INSERT INTO SurveyBucks.LeaderboardEntries (
                                LeaderboardId, UserId, Score, Rank, PreviousRank,
                                IsRewarded, SnapshotDate, CreatedDate
                            )
                            SELECT 
                                @LeaderboardId, tr.UserId, tr.Score, tr.Rank,
                                ISNULL(old.Rank, NULL) AS PreviousRank,
                                0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                            FROM #TempRankings tr
                            LEFT JOIN #OldRankings old ON tr.UserId = old.UserId",
                                new { LeaderboardId = leaderboard.Id }, transaction);

                            // Drop temp tables
                            await connection.ExecuteAsync("DROP TABLE #TempRankings", null, transaction);
                            await connection.ExecuteAsync("DROP TABLE #OldRankings", null, transaction);

                            // If this leaderboard awards points to top performers, process them
                            const string rewardPointsSql = @"
                            SELECT RewardPoints
                            FROM SurveyBucks.Leaderboards
                            WHERE Id = @LeaderboardId AND RewardPoints > 0";

                            var rewardPoints = await connection.ExecuteScalarAsync<int?>(
                                rewardPointsSql, new { LeaderboardId = leaderboard.Id }, transaction);

                            if (rewardPoints.HasValue && rewardPoints.Value > 0)
                            {
                                // Reward top 3 performers
                                const string topPerformersSql = @"
                                SELECT UserId
                                FROM SurveyBucks.LeaderboardEntries
                                WHERE LeaderboardId = @LeaderboardId AND Rank <= 3 AND IsRewarded = 0";

                                var topPerformers = await connection.QueryAsync<string>(
                                    topPerformersSql, new { LeaderboardId = leaderboard.Id }, transaction);

                                foreach (var topUserId in topPerformers)
                                {
                                    // Award points based on rank
                                    int multiplier = 1; // Default for 3rd place

                                    const string getRankSql = @"
                                    SELECT Rank
                                    FROM SurveyBucks.LeaderboardEntries
                                    WHERE LeaderboardId = @LeaderboardId AND UserId = @UserId";

                                    var rank = await connection.ExecuteScalarAsync<int>(
                                        getRankSql, new { LeaderboardId = leaderboard.Id, UserId = topUserId }, transaction);

                                    if (rank == 1)
                                        multiplier = 3; // 1st place gets 3x
                                    else if (rank == 2)
                                        multiplier = 2; // 2nd place gets 2x

                                    int pointsToAward = rewardPoints.Value * multiplier;

                                    // Award points
                                    await connection.ExecuteAsync(
                                        "SurveyBucks.sp_AwardPointsForAction",
                                        new
                                        {
                                            UserId = topUserId,
                                            ActionType = "LeaderboardReward",
                                            ActionCount = multiplier,
                                            ReferenceId = leaderboard.Id.ToString()
                                        },
                                        commandType: CommandType.StoredProcedure,
                                        transaction: transaction);

                                    // Mark as rewarded
                                    await connection.ExecuteAsync(@"
                                    UPDATE SurveyBucks.LeaderboardEntries
                                    SET IsRewarded = 1
                                    WHERE LeaderboardId = @LeaderboardId AND UserId = @UserId",
                                        new { LeaderboardId = leaderboard.Id, UserId = topUserId }, transaction);

                                    // Add notification
                                    const string notificationSql = @"
                                    INSERT INTO SurveyBucks.UserNotification (
                                        UserId, NotificationTypeId, Title, Message,
                                        ReferenceId, ReferenceType, CreatedDate
                                    )
                                    SELECT 
                                        @UserId, Id, 'Leaderboard Reward', 
                                        @Message,
                                        @ReferenceId, 'Leaderboard', SYSDATETIMEOFFSET()
                                    FROM SurveyBucks.NotificationType
                                    WHERE Name = 'PointsAwarded'";

                                    string message = $"Congratulations! You earned {pointsToAward} points for ";
                                    if (rank == 1)
                                        message += "winning 1st place on the ";
                                    else if (rank == 2)
                                        message += "placing 2nd on the ";
                                    else
                                        message += "placing 3rd on the ";

                                    message += leaderboard.TimePeriod + " " + leaderboard.LeaderboardType + " leaderboard!";

                                    await connection.ExecuteAsync(notificationSql,
                                        new
                                        {
                                            UserId = topUserId,
                                            Message = message,
                                            ReferenceId = leaderboard.Id.ToString()
                                        },
                                        transaction);
                                }
                            }
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

        public async Task<bool> ProcessChallengeProgressAsync(string userId, string actionType, int actionValue = 1)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get active challenges for this action type
                        const string challengesSql = @"
                        SELECT c.Id, c.RequiredActionType, c.RequiredActionCount,
                               c.PointsAwarded, c.RewardId,
                               uc.Id AS UserChallengeId, uc.Progress, uc.IsCompleted
                        FROM SurveyBucks.Challenges c
                        LEFT JOIN SurveyBucks.UserChallenges uc ON c.Id = uc.ChallengeId 
                                                                 AND uc.UserId = @UserId
                                                                 AND uc.IsDeleted = 0
                        WHERE c.IsActive = 1 
                          AND c.IsDeleted = 0
                          AND GETDATE() BETWEEN c.StartDate AND c.EndDate
                          AND (uc.IsCompleted IS NULL OR uc.IsCompleted = 0)
                          AND c.RequiredActionType = @ActionType";

                        var challenges = await connection.QueryAsync<(
                            int Id, string RequiredActionType, int RequiredActionCount,
                            int PointsAwarded, int? RewardId,
                            int? UserChallengeId, int? Progress, bool? IsCompleted
                        )>(challengesSql, new { UserId = userId, ActionType = actionType }, transaction);

                        if (!challenges.Any())
                        {
                            transaction.Commit();
                            return false;
                        }

                        bool progressMade = false;

                        foreach (var challenge in challenges)
                        {
                            int newProgress;
                            bool isCompleted;

                            if (challenge.UserChallengeId.HasValue)
                            {
                                // Update existing challenge
                                newProgress = Math.Min(challenge.Progress.Value + actionValue, challenge.RequiredActionCount);
                                isCompleted = newProgress >= challenge.RequiredActionCount;

                                if (newProgress > challenge.Progress || isCompleted != challenge.IsCompleted)
                                {
                                    const string updateSql = @"
                                    UPDATE SurveyBucks.UserChallenges
                                    SET Progress = @Progress,
                                        IsCompleted = @IsCompleted,
                                        CompletedDate = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                                         THEN SYSDATETIMEOFFSET() ELSE CompletedDate END,
                                        PointsAwarded = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                                          THEN @PointsAwarded ELSE PointsAwarded END,
                                        IsRewarded = CASE WHEN @IsCompleted = 1 AND IsCompleted = 0 
                                                       THEN 1 ELSE IsRewarded END,
                                        ModifiedDate = SYSDATETIMEOFFSET()
                                    WHERE Id = @UserChallengeId";

                                    await connection.ExecuteAsync(updateSql,
                                        new
                                        {
                                            UserChallengeId = challenge.UserChallengeId,
                                            Progress = newProgress,
                                            IsCompleted = isCompleted,
                                            PointsAwarded = challenge.PointsAwarded
                                        },
                                        transaction);

                                    progressMade = true;

                                    // If newly completed, add notification and award points/rewards
                                    if (isCompleted && challenge.IsCompleted != true)
                                    {
                                        await ProcessChallengeCompletionAsync(connection, transaction, userId, challenge.Id, challenge.PointsAwarded, challenge.RewardId);
                                    }
                                }
                            }
                            else
                            {
                                // Insert new challenge progress
                                newProgress = Math.Min(actionValue, challenge.RequiredActionCount);
                                isCompleted = newProgress >= challenge.RequiredActionCount;

                                const string insertSql = @"
                                INSERT INTO SurveyBucks.UserChallenges (
                                    UserId, ChallengeId, Progress, IsCompleted,
                                    CompletedDate, PointsAwarded, IsRewarded,
                                    CreatedDate
                                ) VALUES (
                                    @UserId, @ChallengeId, @Progress, @IsCompleted,
                                    CASE WHEN @IsCompleted = 1 THEN SYSDATETIMEOFFSET() ELSE NULL END,
                                    CASE WHEN @IsCompleted = 1 THEN @PointsAwarded ELSE NULL END,
                                    CASE WHEN @IsCompleted = 1 THEN 1 ELSE 0 END,
                                    SYSDATETIMEOFFSET()
                                )";

                                await connection.ExecuteAsync(insertSql,
                                    new
                                    {
                                        UserId = userId,
                                        ChallengeId = challenge.Id,
                                        Progress = newProgress,
                                        IsCompleted = isCompleted,
                                        PointsAwarded = challenge.PointsAwarded
                                    },
                                    transaction);

                                progressMade = true;

                                // If completed on creation, add notification and award points/rewards
                                if (isCompleted)
                                {
                                    await ProcessChallengeCompletionAsync(connection, transaction, userId, challenge.Id, challenge.PointsAwarded, challenge.RewardId);
                                }
                            }
                        }

                        transaction.Commit();
                        return progressMade;
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
