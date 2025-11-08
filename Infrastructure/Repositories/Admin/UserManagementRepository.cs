using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SurveyParticipationSummaryDto = Domain.Models.Admin.SurveyParticipationSummaryDto;

namespace Infrastructure.Repositories.Admin
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserManagementRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<UserAdminDto>> GetAllUsersAsync(int take = 100, int skip = 0)
        {
            const string sql = @"
            SELECT 
                u.Id, u.UserName, u.Email, u.EmailConfirmed, 
                u.FirstName, u.LastName,
                u.IsActive, u.RegistrationDate, u.LastLoginDate,
                ISNULL(ue.TotalSurveysCompleted, 0) AS TotalSurveysCompleted, 
                ISNULL(up.TotalPoints, 0) AS TotalPoints, 
                ISNULL(up.PointsLevel, 1) AS PointsLevel,
                CASE WHEN ISNULL(dps.CompletionPercentage, 0) >= 100 THEN 1 ELSE 0 END AS ProfileComplete,
                
                -- Additional UI fields
                CASE WHEN u.IsActive = 1 AND u.EmailConfirmed = 1 THEN 'Active'
                     WHEN u.IsActive = 1 AND u.EmailConfirmed = 0 THEN 'Pending'
                     ELSE 'Inactive' END AS Status,
                ISNULL(r.Name, 'User') AS Role,
                u.RegistrationDate AS JoinDate,
                u.LastLoginDate AS LastActiveDate,
                ISNULL(CAST(dps.CompletionPercentage AS INT), 0) AS ProfileCompletionPercentage
            FROM SurveyBucks.Users u
            LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
            LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
            LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
            LEFT JOIN SurveyBucks.UserRoles ur ON u.Id = ur.UserId
            LEFT JOIN SurveyBucks.Roles r ON ur.RoleId = r.Id
            ORDER BY u.RegistrationDate DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<UserAdminDto>(sql, new { Skip = skip, Take = take });
            }
        }

        public async Task<UserDetailAdminDto> GetUserDetailsAsync(string userId)
        {
            const string userSql = @"
            SELECT 
                u.Id, u.UserName, u.Email, u.EmailConfirmed, u.IsActive,
                u.RegistrationDate, u.LastLoginDate,
                ue.TotalSurveysCompleted, ue.TotalSurveysStarted, ue.TotalPointsEarned, 
                ue.TotalRewardsRedeemed, ue.CurrentLoginStreak, ue.MaxLoginStreak, ue.TotalLogins,
                up.TotalPoints, up.AvailablePoints, up.RedeemedPoints, up.ExpiredPoints, up.PointsLevel,
                dps.CompletionPercentage,
                dps.RequiredFieldsCompleted, dps.OptionalFieldsCompleted, dps.InterestsAdded,
                ul.Name AS LevelName
            FROM AspNetUsers u
            LEFT JOIN SurveyBucks.UserEngagement ue ON u.Id = ue.UserId
            LEFT JOIN SurveyBucks.UserPoints up ON u.Id = up.UserId
            LEFT JOIN SurveyBucks.DemographicProfileStatus dps ON u.Id = dps.UserId
            LEFT JOIN SurveyBucks.UserLevels ul ON up.PointsLevel = ul.Level
            WHERE u.Id = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var user = await connection.QuerySingleOrDefaultAsync<UserDetailAdminDto>(userSql, new { UserId = userId });

                if (user != null)
                {
                    // Get demographics
                    const string demographicsSql = @"
                    SELECT *
                    FROM SurveyBucks.Demographics
                    WHERE UserId = @UserId AND IsDeleted = 0";

                    user.Demographics = await connection.QuerySingleOrDefaultAsync<DemographicsDto>(demographicsSql, new { UserId = userId });

                    // Get interests
                    const string interestsSql = @"
                    SELECT Id, UserId, Interest, InterestLevel
                    FROM SurveyBucks.UserInterests
                    WHERE UserId = @UserId";

                    user.Interests = (await connection.QueryAsync<UserInterestDto>(interestsSql, new { UserId = userId })).ToList();

                    // Get user engagement
                    const string engagementSql = @"
                    SELECT *
                    FROM SurveyBucks.UserEngagement
                    WHERE UserId = @UserId AND IsDeleted = 0";

                    user.Engagement = await connection.QuerySingleOrDefaultAsync<UserEngagementDto>(engagementSql, new { UserId = userId });

                    // Get recent participations
                    const string participationsSql = @"
                    SELECT 
                        sp.Id, sp.SurveyId, s.Name AS SurveyName,
                        sp.EnrolmentDateTime, sp.StartedAtDateTime, sp.CompletedAtDateTime,
                        ps.Name AS Status, sp.ProgressPercentage, sp.TimeSpentInSeconds
                    FROM SurveyBucks.SurveyParticipation sp
                    JOIN SurveyBucks.Survey s ON sp.SurveyId = s.Id
                    JOIN SurveyBucks.ParticipationStatus ps ON sp.StatusId = ps.Id
                    WHERE sp.UserId = @UserId AND sp.IsDeleted = 0
                    ORDER BY COALESCE(sp.CompletedAtDateTime, sp.StartedAtDateTime, sp.EnrolmentDateTime) DESC
                    OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

                    user.RecentParticipations = (await connection.QueryAsync<SurveyParticipationSummaryDto>(
                        participationsSql, new { UserId = userId })).ToList();

                    // Get recent transactions
                    const string transactionsSql = @"
                    SELECT 
                        pt.Id, pt.UserId, pt.Amount, pt.TransactionType, pt.ActionType,
                        pt.Description, pt.ReferenceId, pt.ReferenceType, pt.TransactionDate, pt.ExpiryDate
                    FROM SurveyBucks.PointTransactions pt
                    WHERE pt.UserId = @UserId AND pt.IsDeleted = 0
                    ORDER BY pt.TransactionDate DESC
                    OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

                    user.RecentPointTransactions = (await connection.QueryAsync<PointTransactionDto>(
                        transactionsSql, new { UserId = userId })).ToList();

                    // Get recent rewards
                    const string rewardsSql = @"
                    SELECT 
                        ur.Id, ur.UserId, ur.RewardId, r.Name AS RewardName,
                        r.Description AS RewardDescription, r.RewardType, r.Amount AS RewardAmount,
                        ur.EarnedDate, ur.RedemptionStatus, ur.ClaimedDate, ur.RedemptionCode,
                        ur.DeliveryStatus
                    FROM SurveyBucks.UserRewards ur
                    JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
                    WHERE ur.UserId = @UserId AND ur.IsDeleted = 0
                    ORDER BY ur.EarnedDate DESC
                    OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

                    user.RecentRewards = (await connection.QueryAsync<UserRewardDto>(
                        rewardsSql, new { UserId = userId })).ToList();
                }

                return user;
            }
        }

        public async Task<IEnumerable<AuditLogDto>> GetUserAuditLogAsync(string userId)
        {
            // This is a placeholder - you would need to implement a proper audit logging system
            const string sql = @"
            -- Get demographic history changes
            SELECT 
                Id, 
                UserId,
                'Demographics' AS Action,
                'Demographics' AS EntityType,
                UserId AS EntityId,
                'Changed ' + FieldName + ' from ''' + ISNULL(OldValue, 'null') + ''' to ''' + ISNULL(NewValue, 'null') + '''' AS Details,
                NULL AS IpAddress,
                ChangeDate AS Timestamp
            FROM SurveyBucks.DemographicHistory
            WHERE UserId = @UserId

            UNION ALL

            -- Get point transactions
            SELECT 
                Id,
                UserId,
                CASE 
                    WHEN TransactionType = 'Earned' THEN 'Points Earned'
                    WHEN TransactionType = 'Redeemed' THEN 'Points Redeemed'
                    WHEN TransactionType = 'Expired' THEN 'Points Expired'
                    ELSE TransactionType
                END AS Action,
                ReferenceType AS EntityType,
                ReferenceId AS EntityId,
                Description + ' (' + CAST(Amount AS NVARCHAR) + ' points)' AS Details,
                NULL AS IpAddress,
                TransactionDate AS Timestamp
            FROM SurveyBucks.PointTransactions
            WHERE UserId = @UserId AND IsDeleted = 0

            UNION ALL

            -- Get survey participations
            SELECT 
                sp.Id,
                sp.UserId,
                CASE
                    WHEN sp.StatusId = 1 THEN 'Enrolled in Survey'
                    WHEN sp.StatusId = 2 THEN 'Started Survey'
                    WHEN sp.StatusId IN (3, 7) THEN 'Completed Survey'
                    WHEN sp.StatusId = 4 THEN 'Abandoned Survey'
                    WHEN sp.StatusId = 5 THEN 'Disqualified from Survey'
                    WHEN sp.StatusId = 6 THEN 'Survey Participation Expired'
                    ELSE 'Survey Participation Updated'
                END AS Action,
                'Survey' AS EntityType,
                CAST(sp.SurveyId AS NVARCHAR) AS EntityId,
                s.Name AS Details,
                NULL AS IpAddress,
                COALESCE(sp.CompletedAtDateTime, sp.StartedAtDateTime, sp.EnrolmentDateTime) AS Timestamp
            FROM SurveyBucks.SurveyParticipation sp
            JOIN SurveyBucks.Survey s ON sp.SurveyId = s.Id
            WHERE sp.UserId = @UserId AND sp.IsDeleted = 0

            UNION ALL

            -- Get reward redemptions
            SELECT 
                ur.Id,
                ur.UserId,
                CASE
                    WHEN ur.RedemptionStatus = 'Unclaimed' THEN 'Reward Earned'
                    WHEN ur.RedemptionStatus = 'Claimed' THEN 'Reward Claimed'
                    ELSE 'Reward Status Updated'
                END AS Action,
                'Reward' AS EntityType,
                CAST(ur.RewardId AS NVARCHAR) AS EntityId,
                r.Name + ' (' + r.RewardType + 
                    CASE WHEN r.Amount IS NOT NULL THEN ', ' + CAST(r.Amount AS NVARCHAR) ELSE '' END + 
                    ')' AS Details,
                NULL AS IpAddress,
                COALESCE(ur.ClaimedDate, ur.EarnedDate) AS Timestamp
            FROM SurveyBucks.UserRewards ur
            JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
            WHERE ur.UserId = @UserId AND ur.IsDeleted = 0

            ORDER BY Timestamp DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<AuditLogDto>(sql, new { UserId = userId });
            }
        }

        public async Task<bool> UpdateUserPointsAsync(string userId, int pointsToAdd, string reason, string modifiedBy)
        {
            if (pointsToAdd == 0)
                return true; // No change needed

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Add point transaction
                        string transactionType = pointsToAdd > 0 ? "Earned" : "Deducted";
                        string actionType = "AdminAdjustment";

                        const string transactionSql = @"
                        INSERT INTO SurveyBucks.PointTransactions (
                            UserId, Amount, TransactionType, ActionType, Description,
                            TransactionDate, CreatedDate, CreatedBy
                        ) VALUES (
                            @UserId, @Amount, @TransactionType, @ActionType, @Description,
                            SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), @CreatedBy
                        )";

                        await connection.ExecuteAsync(transactionSql, new
                        {
                            UserId = userId,
                            Amount = pointsToAdd,
                            TransactionType = transactionType,
                            ActionType = actionType,
                            Description = reason,
                            CreatedBy = modifiedBy
                        }, transaction);

                        // Update user points
                        const string updatePointsSql = @"
                        IF EXISTS (SELECT 1 FROM SurveyBucks.UserPoints WHERE UserId = @UserId)
                        BEGIN
                            UPDATE SurveyBucks.UserPoints
                            SET TotalPoints = TotalPoints + @PointsToAdd,
                                AvailablePoints = AvailablePoints + @PointsToAdd,
                                ModifiedDate = SYSDATETIMEOFFSET(),
                                ModifiedBy = @ModifiedBy
                            WHERE UserId = @UserId;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO SurveyBucks.UserPoints (
                                UserId, TotalPoints, AvailablePoints, RedeemedPoints,
                                ExpiredPoints, PointsLevel, CreatedDate, CreatedBy
                            ) VALUES (
                                @UserId, @PointsToAdd, @PointsToAdd, 0, 0, 1,
                                SYSDATETIMEOFFSET(), @ModifiedBy
                            );
                        END";

                        await connection.ExecuteAsync(updatePointsSql, new
                        {
                            UserId = userId,
                            PointsToAdd = pointsToAdd,
                            ModifiedBy = modifiedBy
                        }, transaction);

                        // Update user engagement stats
                        if (pointsToAdd > 0)
                        {
                            const string updateEngagementSql = @"
                            IF EXISTS (SELECT 1 FROM SurveyBucks.UserEngagement WHERE UserId = @UserId)
                            BEGIN
                                UPDATE SurveyBucks.UserEngagement
                                SET TotalPointsEarned = TotalPointsEarned + @PointsToAdd,
                                    LastActivityDate = SYSDATETIMEOFFSET(),
                                    ModifiedDate = SYSDATETIMEOFFSET()
                                WHERE UserId = @UserId;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO SurveyBucks.UserEngagement (
                                    UserId, TotalPointsEarned, LastActivityDate, CreatedDate
                                ) VALUES (
                                    @UserId, @PointsToAdd, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                                );
                            END";

                            await connection.ExecuteAsync(updateEngagementSql, new
                            {
                                UserId = userId,
                                PointsToAdd = pointsToAdd
                            }, transaction);
                        }

                        // Add notification
                        const string notificationSql = @"
                        INSERT INTO SurveyBucks.UserNotification (
                            UserId, NotificationTypeId, Title, Message, CreatedDate, CreatedBy
                        )
                        SELECT 
                            @UserId, Id, 
                            CASE WHEN @PointsToAdd > 0 THEN 'Points Awarded' ELSE 'Points Deducted' END, 
                            CASE 
                                WHEN @PointsToAdd > 0 THEN @PointsToAdd + ' points have been added to your account: ' + @Reason
                                ELSE ABS(@PointsToAdd) + ' points have been deducted from your account: ' + @Reason
                            END,
                            SYSDATETIMEOFFSET(), @CreatedBy
                        FROM SurveyBucks.NotificationType
                        WHERE Name = 'PointsAwarded'";

                        await connection.ExecuteAsync(notificationSql, new
                        {
                            UserId = userId,
                            PointsToAdd = pointsToAdd,
                            Reason = reason,
                            CreatedBy = modifiedBy
                        }, transaction);

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

        public async Task<bool> BanUserAsync(string userId, string reason, string bannedBy)
        {
            const string sql = @"
            UPDATE AspNetUsers
            SET IsActive = 0,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @BannedBy,
                BanReason = @Reason
            WHERE Id = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { UserId = userId, Reason = reason, BannedBy = bannedBy });
                return result > 0;
            }
        }

        public async Task<bool> UnbanUserAsync(string userId, string unbannedBy)
        {
            const string sql = @"
            UPDATE AspNetUsers
            SET IsActive = 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UnbannedBy,
                BanReason = NULL
            WHERE Id = @UserId";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { UserId = userId, UnbannedBy = unbannedBy });
                return result > 0;
            }
        }
    }
}
