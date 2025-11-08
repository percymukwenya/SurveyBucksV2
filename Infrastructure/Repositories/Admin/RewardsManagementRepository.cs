using Dapper;
using Domain.Interfaces.Repository.Admin;
using Domain.Models.Admin;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Admin
{
    public class RewardsManagementRepository : IRewardsManagementRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public RewardsManagementRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<RewardAdminDto>> GetAllRewardsAsync(bool activeOnly = false)
        {
            string sql = @"
                SELECT 
                    r.Id, r.SurveyId, s.Name AS SurveyName, r.Name, r.Description,
                    r.Amount, r.RewardType, r.RewardCategory, r.PointsCost, r.MonetaryValue,
                    r.ImageUrl, r.MinimumUserLevel, r.AvailableQuantity,
                    r.StartDate, r.EndDate, r.IsActive,
                    COUNT(ur.Id) AS TimesRedeemed
                FROM SurveyBucks.Rewards r
                LEFT JOIN SurveyBucks.Survey s ON r.SurveyId = s.Id
                LEFT JOIN SurveyBucks.UserRewards ur ON r.Id = ur.RewardId AND ur.IsDeleted = 0
                WHERE r.IsDeleted = 0";

            if (activeOnly)
            {
                sql += @" AND r.IsActive = 1
                AND (r.StartDate IS NULL OR r.StartDate <= SYSDATETIMEOFFSET())
                AND (r.EndDate IS NULL OR r.EndDate >= SYSDATETIMEOFFSET())";
            }

            sql += @" GROUP BY 
            r.Id, r.SurveyId, s.Name, r.Name, r.Description,
            r.Amount, r.RewardType, r.RewardCategory, r.PointsCost, r.MonetaryValue,
            r.ImageUrl, r.MinimumUserLevel, r.AvailableQuantity,
            r.StartDate, r.EndDate, r.IsActive
            ORDER BY r.RewardCategory, r.Name";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<RewardAdminDto>(sql);
            }
        }

        public async Task<RewardDetailAdminDto> GetRewardDetailsAsync(int rewardId)
        {
            const string sql = @"
                SELECT 
                    r.Id, r.SurveyId, s.Name AS SurveyName, r.Name, r.Description,
                    r.Amount, r.RewardType, r.RewardCategory, r.PointsCost, r.MonetaryValue,
                    r.ImageUrl, r.MinimumUserLevel, r.AvailableQuantity,
                    r.StartDate, r.EndDate, r.IsActive, r.RedemptionInstructions,
                    r.TermsAndConditions, r.RedemptionUrl, r.IsExternallyFulfilled,
                    r.ExternalReferenceId, r.CreatedDate, r.CreatedBy, r.ModifiedDate, r.ModifiedBy
                FROM SurveyBucks.Rewards r
                LEFT JOIN SurveyBucks.Survey s ON r.SurveyId = s.Id
                WHERE r.Id = @RewardId AND r.IsDeleted = 0";

            const string redemptionSql = @"
                SELECT 
                    ur.Id, ur.UserId, u.UserName AS UserName, ur.EarnedDate, ur.ClaimedDate,
                    ur.RedemptionStatus, ur.DeliveryStatus
                FROM SurveyBucks.UserRewards ur
                JOIN AspNetUsers u ON ur.UserId = u.Id
                WHERE ur.RewardId = @RewardId AND ur.IsDeleted = 0
                ORDER BY ur.EarnedDate DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var reward = await connection.QuerySingleOrDefaultAsync<RewardDetailAdminDto>(sql, new { RewardId = rewardId });

                if (reward != null)
                {
                    reward.Redemptions = (await connection.QueryAsync<UserRewardSummaryDto>(
                        redemptionSql, new { RewardId = rewardId })).ToList();
                }

                return reward;
            }
        }

        public async Task<int> CreateRewardAsync(RewardCreateDto reward, string createdBy)
        {
            const string sql = @"
                INSERT INTO SurveyBucks.Rewards (
                    SurveyId, Name, Description, Amount, RewardType, RewardCategory,
                    PointsCost, MonetaryValue, ImageUrl, RedemptionInstructions,
                    TermsAndConditions, AvailableQuantity, MinimumUserLevel,
                    StartDate, EndDate, RedemptionUrl, IsExternallyFulfilled,
                    ExternalReferenceId, IsActive, CreatedDate, CreatedBy
                ) VALUES (
                    @SurveyId, @Name, @Description, @Amount, @RewardType, @RewardCategory,
                    @PointsCost, @MonetaryValue, @ImageUrl, @RedemptionInstructions,
                    @TermsAndConditions, @AvailableQuantity, @MinimumUserLevel,
                    @StartDate, @EndDate, @RedemptionUrl, @IsExternallyFulfilled,
                    @ExternalReferenceId, @IsActive, SYSDATETIMEOFFSET(), @CreatedBy
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    reward.SurveyId,
                    reward.Name,
                    reward.Description,
                    reward.Amount,
                    reward.RewardType,
                    reward.RewardCategory,
                    reward.PointsCost,
                    reward.MonetaryValue,
                    reward.ImageUrl,
                    reward.RedemptionInstructions,
                    reward.TermsAndConditions,
                    reward.AvailableQuantity,
                    reward.MinimumUserLevel,
                    reward.StartDate,
                    reward.EndDate,
                    reward.RedemptionUrl,
                    reward.IsExternallyFulfilled,
                    reward.ExternalReferenceId,
                    reward.IsActive,
                    CreatedBy = createdBy
                });
            }
        }

        public async Task<bool> UpdateRewardAsync(RewardUpdateDto reward, string modifiedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.Rewards
                SET Name = @Name,
                    Description = @Description,
                    Amount = @Amount,
                    RewardType = @RewardType,
                    RewardCategory = @RewardCategory,
                    PointsCost = @PointsCost,
                    MonetaryValue = @MonetaryValue,
                    ImageUrl = @ImageUrl,
                    RedemptionInstructions = @RedemptionInstructions,
                    TermsAndConditions = @TermsAndConditions,
                    AvailableQuantity = @AvailableQuantity,
                    MinimumUserLevel = @MinimumUserLevel,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    RedemptionUrl = @RedemptionUrl,
                    IsExternallyFulfilled = @IsExternallyFulfilled,
                    ExternalReferenceId = @ExternalReferenceId,
                    IsActive = @IsActive,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @ModifiedBy
                WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    reward.Id,
                    reward.Name,
                    reward.Description,
                    reward.Amount,
                    reward.RewardType,
                    reward.RewardCategory,
                    reward.PointsCost,
                    reward.MonetaryValue,
                    reward.ImageUrl,
                    reward.RedemptionInstructions,
                    reward.TermsAndConditions,
                    reward.AvailableQuantity,
                    reward.MinimumUserLevel,
                    reward.StartDate,
                    reward.EndDate,
                    reward.RedemptionUrl,
                    reward.IsExternallyFulfilled,
                    reward.ExternalReferenceId,
                    reward.IsActive,
                    ModifiedBy = modifiedBy
                });

                return result > 0;
            }
        }

        public async Task<bool> DeleteRewardAsync(int rewardId, string deletedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.Rewards
                SET IsDeleted = 1,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @RewardId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { RewardId = rewardId, DeletedBy = deletedBy });
                return result > 0;
            }
        }



        public async Task<IEnumerable<UserRewardAdminDto>> GetPendingRedemptionsAsync()
        {
            const string sql = @"
                SELECT 
                    ur.Id, ur.UserId, u.UserName, u.Email,
                    ur.RewardId, r.Name AS RewardName, r.RewardType, r.Amount AS RewardAmount,
                    ur.SurveyParticipationId, s.Name AS SurveyName,
                    ur.EarnedDate, ur.RedemptionStatus, ur.ClaimedDate, ur.RedemptionCode,
                    ur.RedemptionMethod, ur.DeliveryStatus, ur.DeliveryDate
                FROM SurveyBucks.UserRewards ur
                JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
                JOIN AspNetUsers u ON ur.UserId = u.Id
                JOIN SurveyBucks.SurveyParticipation sp ON ur.SurveyParticipationId = sp.Id
                JOIN SurveyBucks.Survey s ON sp.SurveyId = s.Id
                WHERE ur.RedemptionStatus = 'Claimed' 
                  AND (ur.DeliveryStatus IS NULL OR ur.DeliveryStatus = 'Pending')
                  AND ur.IsDeleted = 0
                ORDER BY ur.ClaimedDate";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<UserRewardAdminDto>(sql);
            }
        }

        public async Task<bool> ProcessRedemptionAsync(int userRewardId, string status, string processedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.UserRewards
                SET DeliveryStatus = @Status,
                    DeliveryDate = CASE WHEN @Status = 'Delivered' THEN SYSDATETIMEOFFSET() ELSE DeliveryDate END,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @ProcessedBy
                WHERE Id = @UserRewardId AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new
                {
                    UserRewardId = userRewardId,
                    Status = status,
                    ProcessedBy = processedBy
                });

                if (result > 0 && status == "Delivered")
                {
                    // Send notification to user
                    const string notificationSql = @"
                        INSERT INTO SurveyBucks.UserNotification (
                            UserId, NotificationTypeId, Title, Message, CreatedDate, CreatedBy
                        )
                        SELECT 
                            ur.UserId, nt.Id, 
                            'Reward Delivered', 
                            'Your reward ''' + r.Name + ''' has been delivered.',
                            SYSDATETIMEOFFSET(), @ProcessedBy
                        FROM SurveyBucks.UserRewards ur
                        JOIN SurveyBucks.Rewards r ON ur.RewardId = r.Id
                        CROSS JOIN SurveyBucks.NotificationType nt
                        WHERE ur.Id = @UserRewardId AND nt.Name = 'RewardEarned'";

                    await connection.ExecuteAsync(notificationSql, new { UserRewardId = userRewardId, ProcessedBy = processedBy });
                }

                return result > 0;
            }
        }
    }
}
