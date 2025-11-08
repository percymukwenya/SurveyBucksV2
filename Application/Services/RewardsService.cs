using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RewardsService : IRewardsService
    {
        private readonly IRewardsRepository _rewardsRepository;
        private readonly INotificationService _notificationService;
        private readonly IGamificationService _gamificationService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<RewardsService> _logger;

        public RewardsService(
            IRewardsRepository rewardsRepository,
            INotificationService notificationService,
            IGamificationService gamificationService,
            IAnalyticsService analyticsService,
            ILogger<RewardsService> logger)
        {
            _rewardsRepository = rewardsRepository;
            _notificationService = notificationService;
            _gamificationService = gamificationService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // RICH BUSINESS LOGIC - Orchestrates reward redemption process
        public async Task<RewardRedemptionResultDto> RedeemRewardAsync(int rewardId, string userId)
        {
            _logger.LogInformation("User {UserId} attempting to redeem reward {RewardId}", userId, rewardId);

            var result = new RewardRedemptionResultDto();

            try
            {
                // 1. BUSINESS VALIDATION - Get reward details
                var reward = await _rewardsRepository.GetRewardByIdAsync(rewardId);
                if (reward == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Reward not found or inactive";
                    return result;
                }

                // 2. BUSINESS VALIDATION - Check user eligibility
                var eligibilityResult = await ValidateUserEligibilityAsync(userId, reward);
                if (!eligibilityResult.IsEligible)
                {
                    result.Success = false;
                    result.ErrorMessage = eligibilityResult.ErrorMessage;
                    return result;
                }

                // 3. BUSINESS VALIDATION - Check reward availability
                if (reward.AvailableQuantity.HasValue && reward.AvailableQuantity.Value <= 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Reward is out of stock";
                    return result;
                }

                // 4. CORE BUSINESS OPERATION - Process redemption
                var redemptionResult = await ProcessRedemptionAsync(userId, reward);
                if (!redemptionResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = redemptionResult.ErrorMessage;
                    return result;
                }

                result.Success = true;
                result.UserRewardId = redemptionResult.UserRewardId;
                result.RedemptionCode = redemptionResult.RedemptionCode;

                // 5. FIRE-AND-FORGET BUSINESS OPERATIONS
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendRewardRedeemedNotificationAsync(userId, reward.Name, redemptionResult.RedemptionCode);
                        _logger.LogDebug("Reward redemption notification sent: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send reward redemption notification: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gamificationService.ProcessRewardRedemptionAsync(userId, rewardId, reward.PointsCost ?? 0);
                        _logger.LogDebug("Gamification processed for reward redemption: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process gamification for reward redemption: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _analyticsService.TrackRewardRedemptionAsync(rewardId, userId, reward.RewardType);
                        _logger.LogDebug("Analytics tracked for reward redemption: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to track reward redemption analytics: User {UserId}, Reward {RewardId}", userId, rewardId);
                    }
                });

                _logger.LogInformation("User {UserId} successfully redeemed reward {RewardId} with code {RedemptionCode}",
                    userId, rewardId, redemptionResult.RedemptionCode);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reward redemption: User {UserId}, Reward {RewardId}", userId, rewardId);
                result.Success = false;
                result.ErrorMessage = "An error occurred while processing your reward redemption";
                return result;
            }
        }

        // RICH BUSINESS LOGIC - Orchestrates reward claiming process
        public async Task<bool> ClaimRewardAsync(int userRewardId, string userId)
        {
            _logger.LogInformation("User {UserId} claiming reward {UserRewardId}", userId, userRewardId);

            // 1. BUSINESS VALIDATION - Check if reward exists and belongs to user
            var userReward = await _rewardsRepository.GetUserRewardByIdAsync(userRewardId);
            if (userReward == null || userReward.UserId != userId)
            {
                throw new NotFoundException("Reward not found or access denied");
            }

            if (userReward.RedemptionStatus != "Unclaimed")
            {
                _logger.LogInformation("Reward {UserRewardId} already claimed or invalid status", userRewardId);
                return false; // Already claimed or invalid status
            }

            // 2. CORE DATA OPERATION - Claim the reward
            var result = await _rewardsRepository.ClaimUserRewardAsync(userRewardId, userId);

            if (result)
            {
                // 3. FIRE-AND-FORGET BUSINESS OPERATIONS
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendRewardClaimedNotificationAsync(userId, userReward.RewardName);
                        _logger.LogDebug("Reward claimed notification sent: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send reward claimed notification: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gamificationService.ProcessRewardClaimAsync(userId);
                        _logger.LogDebug("Gamification processed for reward claim: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process gamification for reward claim: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _analyticsService.TrackRewardClaimAsync(userRewardId, userId);
                        _logger.LogDebug("Analytics tracked for reward claim: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to track reward claim analytics: User {UserId}, UserReward {UserRewardId}", userId, userRewardId);
                    }
                });

                _logger.LogInformation("User {UserId} successfully claimed reward {UserRewardId}", userId, userRewardId);
            }

            return result;
        }

        // BUSINESS LOGIC - Validate user eligibility for reward
        private async Task<EligibilityResultDto> ValidateUserEligibilityAsync(string userId, RewardDto reward)
        {
            var result = new EligibilityResultDto();

            // Check user points and level
            var userPoints = await _rewardsRepository.GetUserPointsAsync(userId);

            if (userPoints.PointsLevel < (reward.MinimumUserLevel ?? 1))
            {
                result.IsEligible = false;
                result.ErrorMessage = $"Minimum level {reward.MinimumUserLevel} required";
                return result;
            }

            if (reward.PointsCost.HasValue && userPoints.AvailablePoints < reward.PointsCost.Value)
            {
                result.IsEligible = false;
                result.ErrorMessage = $"Insufficient points. Need {reward.PointsCost}, have {userPoints.AvailablePoints}";
                return result;
            }

            // Check if user has completed required survey (for survey-specific rewards)
            if (reward.SurveyId.HasValue)
            {
                var participationId = await _rewardsRepository.GetCompletedParticipationForSurveyAsync(userId, reward.SurveyId.Value);
                if (!participationId.HasValue)
                {
                    result.IsEligible = false;
                    result.ErrorMessage = "Must complete the associated survey first";
                    return result;
                }
            }

            result.IsEligible = true;
            return result;
        }

        // BUSINESS LOGIC - Process the actual redemption transaction
        private async Task<RedemptionProcessResultDto> ProcessRedemptionAsync(string userId, RewardDto reward)
        {
            var result = new RedemptionProcessResultDto();

            try
            {
                // Generate unique redemption code
                var redemptionCode = GenerateRedemptionCode();

                // Create user reward record
                var userReward = new UserRewardDto
                {
                    UserId = userId,
                    RewardId = reward.Id,
                    SurveyId = (int)(reward.SurveyId.HasValue
                        ? await _rewardsRepository.GetCompletedParticipationForSurveyAsync(userId, reward.SurveyId.Value)
                        : null),
                    EarnedDate = DateTime.UtcNow,
                    RedemptionStatus = "Unclaimed",
                    RedemptionCode = redemptionCode,
                    RedemptionMethod = "InApp",
                    DeliveryStatus = "Pending",
                    PointsUsed = reward.PointsCost,
                    MonetaryValueRedeemed = reward.MonetaryValue
                };

                var userRewardId = await _rewardsRepository.CreateUserRewardAsync(userReward);

                // Deduct points if required
                if (reward.PointsCost.HasValue && reward.PointsCost.Value > 0)
                {
                    var pointsDeducted = await _rewardsRepository.DeductUserPointsAsync(
                        userId,
                        reward.PointsCost.Value,
                        "Reward Redemption",
                        reward.Id.ToString());

                    if (!pointsDeducted)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to deduct points - insufficient balance";
                        return result;
                    }

                    // Create points transaction record
                    var transaction = new PointTransactionDto
                    {
                        UserId = userId,
                        Amount = reward.PointsCost.Value * -1, // Negative for deduction
                        TransactionType = "Redeemed",
                        ActionType = "RewardRedemption",
                        Description = $"Points redeemed for {reward.Name}",
                        ReferenceId = reward.Id.ToString(),
                        ReferenceType = "Reward",
                        TransactionDate = DateTime.UtcNow
                    };

                    await _rewardsRepository.CreatePointTransactionAsync(transaction);
                }

                // Update reward quantity if applicable
                if (reward.AvailableQuantity.HasValue)
                {
                    await _rewardsRepository.DecrementRewardQuantityAsync(reward.Id, userId);
                }

                result.Success = true;
                result.UserRewardId = userRewardId;
                result.RedemptionCode = redemptionCode;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessRedemptionAsync for User {UserId}, Reward {RewardId}", userId, reward.Id);
                result.Success = false;
                result.ErrorMessage = "Failed to process redemption";
                return result;
            }
        }

        // BUSINESS LOGIC - Generate unique redemption code
        private string GenerateRedemptionCode()
        {
            return $"RWD-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        // SIMPLE DELEGATION - Pure data access, no business logic needed
        public async Task<UserPointsDto> GetUserPointsAsync(string userId)
        {
            return await _rewardsRepository.GetUserPointsAsync(userId);
        }

        public async Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(string userId, int take = 20, int skip = 0)
        {
            return await _rewardsRepository.GetPointTransactionsAsync(userId, take, skip);
        }

        public async Task<IEnumerable<UserRewardDto>> GetUserRewardsAsync(string userId)
        {
            return await _rewardsRepository.GetUserRewardsAsync(userId);
        }

        public async Task<IEnumerable<RewardDto>> GetAvailableRewardsAsync(string userId)
        {
            return await _rewardsRepository.GetAvailableRewardsAsync(userId);
        }

        public Task<bool> AwardPointsForActionAsync(string userId, string actionType, int actionCount = 1, string referenceId = null)
        {
            throw new NotImplementedException();
        }
    }
}
