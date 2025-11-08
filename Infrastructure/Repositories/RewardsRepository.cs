using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models.Response;
using Infrastructure.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class RewardsRepository : IRewardsRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public RewardsRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserPointsDto> GetUserPointsAsync(string userId)
        {
            const string sql = @"
            SELECT up.Id, up.UserId, up.TotalPoints, up.AvailablePoints, 
                up.RedeemedPoints, up.ExpiredPoints, up.PointsLevel,
                ul.Name AS LevelName, ul.PointsMultiplier,
                ISNULL(nextLevel.PointsRequired - up.TotalPoints, 0) AS PointsToNextLevel
            FROM SurveyBucks.UserPoints up
            JOIN SurveyBucks.UserLevels ul ON up.PointsLevel = ul.Level
            LEFT JOIN SurveyBucks.UserLevels nextLevel ON nextLevel.Level = up.PointsLevel + 1
            WHERE up.UserId = @UserId";

            using var connection = _connectionFactory.CreateConnection();
            var userPoints = await connection.QuerySingleOrDefaultAsync<UserPointsDto>(sql, new { UserId = userId });

            if (userPoints == null)
            {
                // Create default user points record if none exists
                const string insertSql = @"
                INSERT INTO SurveyBucks.UserPoints (
                    UserId, TotalPoints, AvailablePoints, RedeemedPoints,
                    ExpiredPoints, PointsLevel, CreatedDate
                ) VALUES (
                    @UserId, 0, 0, 0, 0, 1, SYSDATETIMEOFFSET()
                );
                
                SELECT up.Id, up.UserId, up.TotalPoints, up.AvailablePoints, 
                    up.RedeemedPoints, up.ExpiredPoints, up.PointsLevel,
                    ul.Name AS LevelName, ul.PointsMultiplier,
                    ISNULL(nextLevel.PointsRequired - up.TotalPoints, 0) AS PointsToNextLevel
                FROM SurveyBucks.UserPoints up
                JOIN SurveyBucks.UserLevels ul ON up.PointsLevel = ul.Level
                LEFT JOIN SurveyBucks.UserLevels nextLevel ON nextLevel.Level = up.PointsLevel + 1
                WHERE up.UserId = @UserId";

                userPoints = await connection.QuerySingleAsync<UserPointsDto>(insertSql, new { UserId = userId });
            }

            return userPoints;
        }

        public async Task<RewardDto> GetRewardByIdAsync(int rewardId)
        {
            const string sql = @"
            SELECT Id, SurveyId, Name, RewardType, Amount, PointsCost, 
                MinimumUserLevel, AvailableQuantity, IsActive,
                RewardCategory, MonetaryValue, ImageUrl, RedemptionInstructions
            FROM SurveyBucks.Rewards
            WHERE Id = @RewardId AND IsActive = 1 AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<RewardDto>(sql, new { RewardId = rewardId });
        }

        public async Task<int> CreateUserRewardAsync(UserRewardDto userReward)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.UserRewards (
                UserId, RewardId, SurveyParticipationId, EarnedDate, 
                RedemptionStatus, RedemptionCode, RedemptionMethod,
                DeliveryStatus, PointsUsed, MonetaryValueRedeemed,
                CreatedDate, CreatedBy
            ) VALUES (
                @UserId, @RewardId, @SurveyParticipationId, @EarnedDate,
                @RedemptionStatus, @RedemptionCode, @RedemptionMethod,
                @DeliveryStatus, @PointsUsed, @MonetaryValueRedeemed,
                @CreatedDate, @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, userReward);
        }

        public async Task<bool> DeductUserPointsAsync(string userId, int pointsToDeduct, string reason, string referenceId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserPoints
            SET AvailablePoints = AvailablePoints - @PointsToDeduct,
                RedeemedPoints = RedeemedPoints + @PointsToDeduct,
                LastPointRedeemedDate = SYSDATETIMEOFFSET(),
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE UserId = @UserId AND AvailablePoints >= @PointsToDeduct";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                PointsToDeduct = pointsToDeduct
            });

            return result > 0;
        }

        public async Task<bool> CreatePointTransactionAsync(PointTransactionDto transaction)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.PointTransactions (
                UserId, Amount, TransactionType, ActionType,
                Description, ReferenceId, ReferenceType,
                TransactionDate, CreatedDate, CreatedBy
            ) VALUES (
                @UserId, @Amount, @TransactionType, @ActionType,
                @Description, @ReferenceId, @ReferenceType,
                @TransactionDate, @CreatedDate, @CreatedBy
            )";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, transaction);
            return result > 0;
        }

        // SIMPLIFIED - Just update reward quantity
        public async Task<bool> DecrementRewardQuantityAsync(int rewardId, string modifiedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.Rewards
            SET AvailableQuantity = AvailableQuantity - 1,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @ModifiedBy
            WHERE Id = @RewardId AND AvailableQuantity > 0";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { RewardId = rewardId, ModifiedBy = modifiedBy });
            return result > 0;
        }

        public async Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(string userId, int take = 20, int skip = 0)
        {
            const string sql = @"
            SELECT Id, UserId, Amount, TransactionType, ActionType, 
                   Description, ReferenceId, ReferenceType, TransactionDate, ExpiryDate
            FROM SurveyBucks.PointTransactions
            WHERE UserId = @UserId AND IsDeleted = 0
            ORDER BY TransactionDate DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<PointTransactionDto>(
                    sql, new { UserId = userId, Skip = skip, Take = take });
            }
        }

        public async Task<IEnumerable<UserRewardDto>> GetUserRewardsAsync(string userId)
        {
            const string sql = @"
            SELECT ur.Id, ur.UserId, ur.RewardId, r.Name AS RewardName, 
                   r.Description AS RewardDescription, r.RewardType, r.Amount AS RewardAmount,
                   ur.EarnedDate, ur.RedemptionStatus, ur.ClaimedDate, ur.RedemptionCode,
                   ur.DeliveryStatus
            FROM SurveyBucks.UserRewards ur
            JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
            WHERE ur.UserId = @UserId AND ur.IsDeleted = 0
            ORDER BY ur.EarnedDate DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<UserRewardDto>(sql, new { UserId = userId });
            }
        }

        public async Task<int?> GetCompletedParticipationForSurveyAsync(string userId, int surveyId)
        {
            const string sql = @"
            SELECT TOP 1 Id 
            FROM SurveyBucks.SurveyParticipation 
            WHERE SurveyId = @SurveyId AND UserId = @UserId 
                AND StatusId IN (3, 7) -- Completed or Rewarded
                AND IsDeleted = 0
            ORDER BY CompletedAtDateTime DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { UserId = userId, SurveyId = surveyId });
        }

        public async Task<bool> ClaimUserRewardAsync(int userRewardId, string userId)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserRewards
            SET RedemptionStatus = 'Claimed',
                ClaimedDate = SYSDATETIMEOFFSET(),
                DeliveryStatus = 'Processing',
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @UserId
            WHERE Id = @UserRewardId AND UserId = @UserId AND RedemptionStatus = 'Unclaimed'";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { UserRewardId = userRewardId, UserId = userId });
            return result > 0;
        }



        public async Task<bool> RedeemRewardAsync(int rewardId, string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get reward details
                        const string rewardSql = @"
                        SELECT Id, SurveyId, Name, RewardType, Amount, PointsCost, 
                               MinimumUserLevel, AvailableQuantity
                        FROM SurveyBucks.Rewards
                        WHERE Id = @RewardId AND IsActive = 1 AND IsDeleted = 0";

                        var reward = await connection.QuerySingleOrDefaultAsync<RewardDto>(
                            rewardSql, new { RewardId = rewardId }, transaction);

                        if (reward == null)
                        {
                            return false; // Reward not found or not active
                        }

                        // Check user eligibility
                        const string userPointsSql = @"
                        SELECT AvailablePoints, PointsLevel
                        FROM SurveyBucks.UserPoints
                        WHERE UserId = @UserId";

                        var userPoints = await connection.QuerySingleOrDefaultAsync<(int AvailablePoints, int PointsLevel)>(
                            userPointsSql, new { UserId = userId }, transaction);

                        if (userPoints.PointsLevel < (reward.MinimumUserLevel ?? 1))
                        {
                            return false; // User level too low
                        }

                        if (reward.PointsCost.HasValue && userPoints.AvailablePoints < reward.PointsCost.Value)
                        {
                            return false; // Not enough points
                        }

                        // Check if reward has quantity limit
                        if (reward.AvailableQuantity.HasValue && reward.AvailableQuantity.Value <= 0)
                        {
                            return false; // Out of stock
                        }

                        // Generate redemption code
                        var redemptionCode = $"RWD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                        // Create user reward record
                        const string insertRewardSql = @"
                        INSERT INTO SurveyBucks.UserRewards (
                            UserId, RewardId, SurveyParticipationId, EarnedDate, 
                            RedemptionStatus, RedemptionCode, RedemptionMethod, 
                            DeliveryStatus, CreatedDate, CreatedBy
                        )
                        SELECT @UserId, @RewardId, 
                               (SELECT TOP 1 Id FROM SurveyBucks.SurveyParticipation 
                                WHERE SurveyId = @SurveyId AND UserId = @UserId 
                                AND StatusId IN (3, 7) -- Completed or Rewarded
                                ORDER BY CompletedAtDateTime DESC),
                               SYSDATETIMEOFFSET(), 'Unclaimed', @RedemptionCode,
                               'InApp', 'Pending', SYSDATETIMEOFFSET(), @UserId";

                        await connection.ExecuteAsync(insertRewardSql,
                            new
                            {
                                UserId = userId,
                                RewardId = rewardId,
                                SurveyId = reward.SurveyId,
                                RedemptionCode = redemptionCode
                            }, transaction);

                        // Deduct points if needed
                        if (reward.PointsCost.HasValue && reward.PointsCost.Value > 0)
                        {
                            const string updatePointsSql = @"
                            UPDATE SurveyBucks.UserPoints
                            SET AvailablePoints = AvailablePoints - @PointsCost,
                                RedeemedPoints = RedeemedPoints + @PointsCost,
                                LastPointRedeemedDate = SYSDATETIMEOFFSET(),
                                ModifiedDate = SYSDATETIMEOFFSET(),
                                ModifiedBy = @UserId
                            WHERE UserId = @UserId";

                            await connection.ExecuteAsync(updatePointsSql,
                                new { UserId = userId, PointsCost = reward.PointsCost.Value }, transaction);

                            // Add points transaction record
                            const string pointTransactionSql = @"
                            INSERT INTO SurveyBucks.PointTransactions (
                                UserId, Amount, TransactionType, ActionType,
                                Description, ReferenceId, ReferenceType,
                                TransactionDate
                            ) VALUES (
                                @UserId, @Amount, 'Redeemed', 'RewardRedemption',
                                @Description, @ReferenceId, 'Reward',
                                SYSDATETIMEOFFSET()
                            )";

                            await connection.ExecuteAsync(pointTransactionSql,
                                new
                                {
                                    UserId = userId,
                                    Amount = reward.PointsCost.Value * -1, // Negative to show deduction
                                    Description = $"Points redeemed for {reward.Name}",
                                    ReferenceId = reward.Id.ToString()
                                }, transaction);
                        }

                        // Update reward quantity if applicable
                        if (reward.AvailableQuantity.HasValue)
                        {
                            const string updateQuantitySql = @"
                            UPDATE SurveyBucks.Rewards
                            SET AvailableQuantity = AvailableQuantity - 1,
                                ModifiedDate = SYSDATETIMEOFFSET(),
                                ModifiedBy = @UserId
                            WHERE Id = @RewardId AND AvailableQuantity > 0";

                            await connection.ExecuteAsync(updateQuantitySql,
                                new { RewardId = rewardId, UserId = userId }, transaction);
                        }

                        // Add notification
                        const string notificationSql = @"
                        INSERT INTO SurveyBucks.UserNotification (
                            UserId, NotificationTypeId, Title, Message,
                            ReferenceId, ReferenceType, CreatedDate
                        )
                        SELECT 
                            @UserId, Id, 'Reward Ready', 
                            @Message,
                            @ReferenceId, 'Reward', SYSDATETIMEOFFSET()
                        FROM SurveyBucks.NotificationType
                        WHERE Name = 'RewardEarned'";

                        await connection.ExecuteAsync(notificationSql,
                            new
                            {
                                UserId = userId,
                                Message = $"Your {reward.Name} reward is ready to be claimed!",
                                ReferenceId = rewardId.ToString()
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

        public async Task<bool> ClaimRewardAsync(int userRewardId, string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if the reward exists and belongs to the user
                        const string checkSql = @"
                        SELECT ur.Id, ur.RedemptionStatus, r.Name
                        FROM SurveyBucks.UserRewards ur
                        JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
                        WHERE ur.Id = @UserRewardId AND ur.UserId = @UserId AND ur.IsDeleted = 0";

                        var reward = await connection.QuerySingleOrDefaultAsync<(int Id, string RedemptionStatus, string Name)>(
                            checkSql, new { UserRewardId = userRewardId, UserId = userId }, transaction);

                        if (reward.Id == 0 || reward.RedemptionStatus != "Unclaimed")
                        {
                            return false; // Reward not found or already claimed
                        }

                        // Update the reward status
                        const string updateSql = @"
                        UPDATE SurveyBucks.UserRewards
                        SET RedemptionStatus = 'Claimed',
                            ClaimedDate = SYSDATETIMEOFFSET(),
                            DeliveryStatus = 'Processing',
                            ModifiedDate = SYSDATETIMEOFFSET(),
                            ModifiedBy = @UserId
                        WHERE Id = @UserRewardId AND UserId = @UserId";

                        await connection.ExecuteAsync(updateSql,
                            new { UserRewardId = userRewardId, UserId = userId }, transaction);

                        // Add notification
                        const string notificationSql = @"
                        INSERT INTO SurveyBucks.UserNotification (
                            UserId, NotificationTypeId, Title, Message,
                            ReferenceId, ReferenceType, CreatedDate
                        )
                        SELECT 
                            @UserId, Id, 'Reward Claimed', 
                            @Message,
                            @ReferenceId, 'Reward', SYSDATETIMEOFFSET()
                        FROM SurveyBucks.NotificationType
                        WHERE Name = 'RewardEarned'";

                        await connection.ExecuteAsync(notificationSql,
                            new
                            {
                                UserId = userId,
                                Message = $"You've successfully claimed your {reward.Name} reward!",
                                ReferenceId = userRewardId.ToString()
                            }, transaction);

                        // Update user stats
                        const string updateStatsSql = @"
                        UPDATE SurveyBucks.UserEngagement
                        SET TotalRewardsRedeemed = TotalRewardsRedeemed + 1,
                            LastActivityDate = SYSDATETIMEOFFSET(),
                            ModifiedDate = SYSDATETIMEOFFSET()
                        WHERE UserId = @UserId";

                        await connection.ExecuteAsync(updateStatsSql, new { UserId = userId }, transaction);

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

        public async Task<IEnumerable<RewardDto>> GetAvailableRewardsAsync(string userId)
        {
            const string sql = @"
            SELECT r.Id, r.Name, r.Description, r.RewardType, r.Amount, 
                   r.PointsCost, r.MinimumUserLevel, r.AvailableQuantity
            FROM SurveyBucks.Rewards r
            WHERE r.IsActive = 1 AND r.IsDeleted = 0
            ORDER BY r.Name";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<RewardDto>(sql);
        }

        public async Task<UserRewardDto> GetUserRewardByIdAsync(int userRewardId)
        {
            const string sql = @"
            SELECT ur.Id, ur.UserId, ur.RewardId, ur.PointsUsed, ur.ClaimDate, ur.Status,
                   ur.ClaimedDate, ur.RejectedDate, ur.RejectionReason,
                   r.Name AS RewardName, r.Description AS RewardDescription,
                   r.RewardType, r.Amount
            FROM SurveyBucks.UserRewards ur
            INNER JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
            WHERE ur.Id = @UserRewardId AND ur.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<UserRewardDto>(sql, new { UserRewardId = userRewardId });
        }
    }
}
