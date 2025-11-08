using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IUserEngagementRepository _engagementRepository;
        private readonly IUserAchievementRepository _achievementRepository;
        private readonly IUserChallengeRepository _challengeRepository;
        private readonly IUserPointsRepository _pointsRepository;
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<GamificationService> _logger;

        public GamificationService(
            IUserEngagementRepository engagementRepository,
            IUserAchievementRepository achievementRepository,
            IUserChallengeRepository challengeRepository,
            IUserPointsRepository pointsRepository,
            ILeaderboardRepository leaderboardRepository,
            INotificationService notificationService,
            ILogger<GamificationService> logger)
        {
            _engagementRepository = engagementRepository;
            _achievementRepository = achievementRepository;
            _challengeRepository = challengeRepository;
            _pointsRepository = pointsRepository;
            _leaderboardRepository = leaderboardRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> UpdateLoginStreakAsync(string userId)
        {
            _logger.LogDebug("Processing login streak for user {UserId}", userId);

            try
            {
                var engagement = await _engagementRepository.GetUserEngagementAsync(userId);
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);

                int newStreak;
                int maxStreak;

                // Business logic for calculating streak
                if (engagement.LastLoginDate?.Date == today)
                {
                    // Already logged in today, no change
                    return true;
                }
                else if (engagement.LastLoginDate?.Date == yesterday)
                {
                    // Consecutive day login - increment streak
                    newStreak = engagement.CurrentLoginStreak + 1;
                }
                else
                {
                    // Streak broken or first login - reset to 1
                    newStreak = 1;
                }

                maxStreak = Math.Max(newStreak, engagement.MaxLoginStreak);

                // Update data
                var result = await _engagementRepository.UpdateLoginStreakAsync(userId, newStreak, maxStreak, today);

                if (result)
                {
                    // Fire-and-forget business operations
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessLoginAchievementsAsync(userId, newStreak, maxStreak);
                            await ProcessLoginChallengesAsync(userId);
                            await AwardLoginPointsAsync(userId, newStreak);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process login gamification for user {UserId}", userId);
                        }
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating login streak for user {UserId}", userId);
                return false;
            }
        }

        public async Task ProcessEnrollmentAsync(string userId, int surveyId)
        {
            _logger.LogDebug("Processing enrollment gamification for user {UserId}, survey {SurveyId}", userId, surveyId);

            try
            {
                // Award enrollment points
                await _pointsRepository.AddPointsAsync(userId, GetEnrollmentPoints(), "SurveyEnrollment", surveyId.ToString());

                // Fire-and-forget operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessEnrollmentAchievementsAsync(userId);
                        await ProcessEnrollmentChallengesAsync(userId);
                        await UpdateEngagementStatsAsync(userId, isEnrollment: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process enrollment gamification for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enrollment for user {UserId}, survey {SurveyId}", userId, surveyId);
            }
        }

        public async Task ProcessSurveyCompletionAsync(string userId, int surveyId)
        {
            _logger.LogDebug("Processing completion gamification for user {UserId}, survey {SurveyId}", userId, surveyId);

            try
            {
                // Award completion points (higher than enrollment)
                await _pointsRepository.AddPointsAsync(userId, GetCompletionPoints(), "SurveyCompletion", surveyId.ToString());

                // Fire-and-forget operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessCompletionAchievementsAsync(userId);
                        await ProcessCompletionChallengesAsync(userId);
                        await UpdateEngagementStatsAsync(userId, isCompletion: true);
                        await CheckLevelUpAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process completion gamification for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing completion for user {UserId}, survey {SurveyId}", userId, surveyId);
            }
        }

        public async Task ProcessQuestionAnsweredAsync(string userId, int questionId)
        {
            _logger.LogDebug("Processing question answered for user {UserId}, question {QuestionId}", userId, questionId);

            try
            {
                // Small points for question answers to encourage engagement
                await _pointsRepository.AddPointsAsync(userId, GetQuestionPoints(), "QuestionAnswered", questionId.ToString());

                // Fire-and-forget operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessQuestionChallengesAsync(userId);
                        await CheckEngagementMilestonesAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process question gamification for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing question answered for user {UserId}, question {QuestionId}", userId, questionId);
            }
        }

        // RICH BUSINESS LOGIC - Progress update processing
        public async Task ProcessProgressUpdateAsync(string userId, int progressPercentage)
        {
            _logger.LogDebug("Processing progress update for user {UserId}, progress {Progress}%", userId, progressPercentage);

            try
            {
                // Fire-and-forget milestone checking
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessProgressMilestonesAsync(userId, progressPercentage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process progress milestones for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing progress update for user {UserId}", userId);
            }
        }

        // RICH BUSINESS LOGIC - Reward redemption processing
        public async Task ProcessRewardRedemptionAsync(string userId, int rewardId, int pointsUsed)
        {
            _logger.LogDebug("Processing reward redemption for user {UserId}, reward {RewardId}", userId, rewardId);

            try
            {
                // Fire-and-forget operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessRedemptionAchievementsAsync(userId);
                        await ProcessSpendingMilestonesAsync(userId, pointsUsed);
                        await ProcessRedemptionChallengesAsync(userId);
                        await UpdateEngagementStatsAsync(userId, isRedemption: true);

                        // Award bonus points for first redemption
                        var redemptionCount = await _pointsRepository.GetActionCountAsync(userId, "RewardRedemption");
                        if (redemptionCount == 1)
                        {
                            await _pointsRepository.AddPointsAsync(userId, GetFirstRedemptionBonus(), "FirstRedemptionBonus", rewardId.ToString());
                            await _notificationService.SendFirstRedemptionBonusNotificationAsync(userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process redemption gamification for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reward redemption for user {UserId}, reward {RewardId}", userId, rewardId);
            }
        }

        // RICH BUSINESS LOGIC - Reward claim processing
        public async Task ProcessRewardClaimAsync(string userId)
        {
            _logger.LogDebug("Processing reward claim for user {UserId}", userId);

            try
            {
                // Award small points for claiming rewards promptly
                await _pointsRepository.AddPointsAsync(userId, GetClaimPoints(), "RewardClaim", userId);

                // Fire-and-forget operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessClaimAchievementsAsync(userId);
                        await ProcessClaimChallengesAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process claim gamification for user {UserId}", userId);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reward claim for user {UserId}", userId);
            }
        }

        // BUSINESS LOGIC - Achievement processing methods
        private async Task ProcessLoginAchievementsAsync(string userId, int currentStreak, int maxStreak)
        {
            var streakAchievements = new Dictionary<int, (string Key, int Points)>
            {
                [3] = ("ConsecutiveLogins3", 30),
                [7] = ("ConsecutiveLogins7", 70),
                [14] = ("ConsecutiveLogins14", 150),
                [30] = ("ConsecutiveLogins30", 300),
                [60] = ("ConsecutiveLogins60", 600),
                [100] = ("ConsecutiveLogins100", 1000)
            };

            foreach (var (streak, (achievementKey, points)) in streakAchievements)
            {
                if (currentStreak == streak)
                {
                    await UnlockAchievementAsync(userId, achievementKey, points);
                    break; // Only unlock one achievement per login
                }
            }

            // Check max streak achievements
            var maxStreakAchievements = new Dictionary<int, (string Key, int Points)>
            {
                [7] = ("MaxStreak7", 100),
                [30] = ("MaxStreak30", 500),
                [100] = ("MaxStreak100", 2000)
            };

            foreach (var (maxStreakTarget, (achievementKey, points)) in maxStreakAchievements)
            {
                if (maxStreak >= maxStreakTarget)
                {
                    var hasAchievement = await _achievementRepository.HasAchievementAsync(userId, achievementKey);
                    if (!hasAchievement)
                    {
                        await UnlockAchievementAsync(userId, achievementKey, points);
                    }
                }
            }
        }

        private async Task ProcessEnrollmentAchievementsAsync(string userId)
        {
            var enrollmentCount = await _pointsRepository.GetActionCountAsync(userId, "SurveyEnrollment");

            var enrollmentAchievements = new Dictionary<int, (string Key, int Points)>
            {
                [1] = ("FirstEnrollment", 50),
                [5] = ("SurveyExplorer", 100),
                [10] = ("SurveySeeker", 200),
                [25] = ("SurveyHunter", 500),
                [50] = ("SurveyCollector", 1000),
                [100] = ("SurveyMaster", 2000)
            };

            if (enrollmentAchievements.TryGetValue(enrollmentCount, out var achievement))
            {
                await UnlockAchievementAsync(userId, achievement.Key, achievement.Points);
            }
        }

        private async Task ProcessCompletionAchievementsAsync(string userId)
        {
            var completionCount = await _pointsRepository.GetActionCountAsync(userId, "SurveyCompletion");

            var completionAchievements = new Dictionary<int, (string Key, int Points)>
            {
                [1] = ("FirstCompletion", 100),
                [5] = ("SurveyNovice", 250),
                [10] = ("SurveyAdept", 500),
                [25] = ("SurveyExpert", 1000),
                [50] = ("SurveyPro", 2000),
                [100] = ("SurveyLegend", 5000)
            };

            if (completionAchievements.TryGetValue(completionCount, out var achievement))
            {
                await UnlockAchievementAsync(userId, achievement.Key, achievement.Points);
            }
        }

        private async Task ProcessRedemptionAchievementsAsync(string userId)
        {
            var redemptionCount = await _pointsRepository.GetActionCountAsync(userId, "RewardRedemption");

            var redemptionAchievements = new Dictionary<int, (string Key, int Points)>
            {
                [1] = ("FirstRedemption", 75),
                [5] = ("ShopperNovice", 150),
                [10] = ("ShopperExpert", 300),
                [25] = ("ShopperMaster", 750),
                [50] = ("ShopperLegend", 1500)
            };

            if (redemptionAchievements.TryGetValue(redemptionCount, out var achievement))
            {
                await UnlockAchievementAsync(userId, achievement.Key, achievement.Points);
            }
        }

        private async Task ProcessClaimAchievementsAsync(string userId)
        {
            var claimCount = await _pointsRepository.GetActionCountAsync(userId, "RewardClaim");

            if (claimCount == 1)
            {
                await UnlockAchievementAsync(userId, "FirstClaim", 25);
            }
            else if (claimCount == 10)
            {
                await UnlockAchievementAsync(userId, "RewardCollector", 100);
            }
        }

        private async Task ProcessProgressMilestonesAsync(string userId, int progressPercentage)
        {
            var milestones = new[] { 25, 50, 75 };

            foreach (var milestone in milestones)
            {
                if (progressPercentage == milestone)
                {
                    var achievementKey = $"Progress{milestone}Percent";
                    var points = milestone; // 25, 50, 75 points respectively
                    await UnlockAchievementAsync(userId, achievementKey, points);
                }
            }
        }

        private async Task ProcessSpendingMilestonesAsync(string userId, int pointsSpent)
        {
            var totalSpent = await _pointsRepository.GetTotalPointsSpentAsync(userId);

            var milestones = new Dictionary<int, (string Key, int Points)>
            {
                [100] = ("Spender100", 50),
                [500] = ("Spender500", 100),
                [1000] = ("Spender1000", 200),
                [5000] = ("Spender5000", 500),
                [10000] = ("Spender10000", 1000)
            };

            foreach (var (spendingTarget, (achievementKey, bonusPoints)) in milestones)
            {
                if (totalSpent >= spendingTarget && (totalSpent - pointsSpent) < spendingTarget)
                {
                    // Just crossed this milestone
                    await UnlockAchievementAsync(userId, achievementKey, bonusPoints);
                }
            }
        }

        // BUSINESS LOGIC - Challenge processing methods
        private async Task ProcessEnrollmentChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "SurveyEnrollment", 1);
        }

        private async Task ProcessCompletionChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "SurveyCompletion", 1);
        }

        private async Task ProcessQuestionChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "QuestionAnswered", 1);
        }

        private async Task ProcessRedemptionChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "RewardRedemption", 1);
        }

        private async Task ProcessClaimChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "RewardClaim", 1);
        }

        private async Task ProcessLoginChallengesAsync(string userId)
        {
            await UpdateActiveChallengesAsync(userId, "ConsecutiveLogins", 1);
        }

        private async Task UpdateActiveChallengesAsync(string userId, string actionType, int actionValue)
        {
            var challenges = await _challengeRepository.GetActiveChallengesAsync(userId);
            var relevantChallenges = challenges.Where(c => c.RequiredActionType == actionType && !c.IsCompleted);

            foreach (var challenge in relevantChallenges)
            {
                var currentProgress = challenge.Progress;
                var newProgress = Math.Min(currentProgress + actionValue, challenge.RequiredActionCount);
                var isCompleted = newProgress >= challenge.RequiredActionCount;

                if (newProgress > currentProgress || isCompleted)
                {
                    await _challengeRepository.UpdateChallengeProgressAsync(
                        userId,
                        challenge.Id,
                        newProgress,
                        isCompleted,
                        isCompleted ? challenge.PointsAwarded : null);

                    if (isCompleted && !challenge.IsCompleted)
                    {
                        await ProcessChallengeCompletionAsync(userId, challenge);
                    }
                }
            }
        }

        private async Task ProcessChallengeCompletionAsync(string userId, ChallengeDto challenge)
        {
            // Award challenge points
            if (challenge.PointsAwarded > 0)
            {
                await _pointsRepository.AddPointsAsync(userId, (int)challenge.PointsAwarded, "ChallengeCompletion", challenge.Id.ToString());
            }

            // Send notification
            await _notificationService.SendChallengeCompletedNotificationAsync(userId, challenge.Name, challenge.PointsAwarded);

            // Award bonus achievement for challenge completion
            var challengeCompletions = await _pointsRepository.GetActionCountAsync(userId, "ChallengeCompletion");
            if (challengeCompletions == 1)
            {
                await UnlockAchievementAsync(userId, "FirstChallenge", 100);
            }
            else if (challengeCompletions == 10)
            {
                await UnlockAchievementAsync(userId, "ChallengeChampion", 500);
            }
        }

        // BUSINESS LOGIC - Level and engagement methods
        private async Task CheckLevelUpAsync(string userId)
        {
            var currentLevel = await _pointsRepository.GetUserLevelAsync(userId);
            var totalPoints = await _pointsRepository.GetTotalPointsAsync(userId);
            var newLevel = CalculateLevel(totalPoints);

            if (newLevel > currentLevel)
            {
                await _pointsRepository.UpdateUserLevelAsync(userId, newLevel);
                await _notificationService.SendLevelUpNotificationAsync(userId, newLevel);

                // Award level up bonus points
                var bonusPoints = GetLevelUpBonus(newLevel);
                if (bonusPoints > 0)
                {
                    await _pointsRepository.AddPointsAsync(userId, bonusPoints, "LevelUpBonus", newLevel.ToString());
                }

                _logger.LogInformation("User {UserId} leveled up to level {Level}", userId, newLevel);
            }
        }

        private async Task UpdateEngagementStatsAsync(string userId, bool isEnrollment = false, bool isCompletion = false, bool isRedemption = false)
        {
            if (isEnrollment)
            {
                // Update is handled in survey participation - no need to duplicate
            }
            else if (isCompletion)
            {
                await _engagementRepository.UpdateSurveyStatsAsync(userId, isCompletion: true);
            }
            else if (isRedemption)
            {
                await _engagementRepository.UpdateRewardsRedeemedAsync(userId);
            }
        }

        private async Task CheckEngagementMilestonesAsync(string userId)
        {
            var engagement = await _engagementRepository.GetUserEngagementAsync(userId);

            // Check for engagement achievements based on total activity
            var totalActions = engagement.TotalSurveysCompleted + engagement.TotalRewardsRedeemed;

            if (totalActions == 50)
            {
                await UnlockAchievementAsync(userId, "ActiveUser", 200);
            }
            else if (totalActions == 100)
            {
                await UnlockAchievementAsync(userId, "SuperUser", 500);
            }
        }

        private async Task AwardLoginPointsAsync(string userId, int streak)
        {
            // Award base login points plus streak bonus
            var basePoints = GetLoginPoints();
            var streakBonus = Math.Min(streak - 1, 10); // Up to 10 bonus points
            var totalPoints = basePoints + streakBonus;

            await _pointsRepository.AddPointsAsync(userId, totalPoints, "DailyLogin", streak.ToString());
        }

        // Helper method to unlock achievements with notification
        private async Task UnlockAchievementAsync(string userId, string achievementKey, int points)
        {
            var achievement = await _achievementRepository.GetAchievementByKeyAsync(achievementKey);
            if (achievement != null)
            {
                var unlocked = await _achievementRepository.UnlockAchievementAsync(userId, achievementKey, points);
                if (unlocked)
                {
                    await _notificationService.SendAchievementNotificationAsync(userId, achievementKey);
                    await _pointsRepository.AddPointsAsync(userId, points, "AchievementUnlock", achievement.Id.ToString());

                    _logger.LogInformation("User {UserId} unlocked achievement {Achievement} for {Points} points",
                        userId, achievementKey, points);
                }
            }
        }


        // BUSINESS LOGIC - Point calculation methods
        private int GetEnrollmentPoints() => 10;
        private int GetCompletionPoints() => 100;
        private int GetQuestionPoints() => 5;
        private int GetLoginPoints() => 5;
        private int GetClaimPoints() => 10;
        private int GetFirstRedemptionBonus() => 50;
        private int GetLevelUpBonus(int level) => level * 25; // 25, 50, 75, etc.

        private int CalculateLevel(int totalPoints)
        {
            // Progressive level calculation - gets harder as you level up
            if (totalPoints < 100) return 1;
            if (totalPoints < 300) return 2;
            if (totalPoints < 600) return 3;
            if (totalPoints < 1000) return 4;
            if (totalPoints < 1500) return 5;
            if (totalPoints < 2500) return 6;
            if (totalPoints < 4000) return 7;
            if (totalPoints < 6000) return 8;
            if (totalPoints < 9000) return 9;
            if (totalPoints < 13000) return 10;

            // Beyond level 10, each level requires 5000 more points
            return 10 + ((totalPoints - 13000) / 5000);
        }

        // SIMPLE DELEGATION - Methods that just return data
        public async Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(string userId)
        {
            return await _challengeRepository.GetActiveChallengesAsync(userId);
        }

        public async Task<IEnumerable<LeaderboardSummaryDto>> GetAvailableLeaderboardsAsync(string userId)
        {
            return await _leaderboardRepository.GetAvailableLeaderboardsAsync(userId);
        }

        public async Task<LeaderboardDto> GetLeaderboardAsync(int leaderboardId, string userId, int top = 10)
        {
            return await _leaderboardRepository.GetLeaderboardAsync(leaderboardId, userId, top);
        }

        public async Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(string userId)
        {
            return await _achievementRepository.GetUserAchievementsAsync(userId);
        }

        public async Task<UserLevelDto> GetUserLevelAsync(string userId)
        {
            var totalPoints = await _pointsRepository.GetTotalPointsAsync(userId);
            var currentLevel = await _pointsRepository.GetUserLevelAsync(userId);
            var nextLevel = currentLevel + 1;

            // Calculate points needed for next level
            var pointsForNextLevel = GetPointsRequiredForLevel(nextLevel);
            var pointsToNextLevel = Math.Max(0, pointsForNextLevel - totalPoints);

            // Calculate progress percentage
            var pointsForCurrentLevel = GetPointsRequiredForLevel(currentLevel);
            var progressPercentage = nextLevel <= 10
                ? (int)((totalPoints - pointsForCurrentLevel) * 100.0 / (pointsForNextLevel - pointsForCurrentLevel))
                : Math.Min(100, (int)((totalPoints - pointsForCurrentLevel) * 100.0 / 5000));

            return new UserLevelDto
            {
                Level = currentLevel,
                Name = GetLevelName(currentLevel),
                CurrentUserPoints = totalPoints,
                PointsToNextLevel = pointsToNextLevel,
                ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage))
            };
        }

        private int GetPointsRequiredForLevel(int level)
        {
            return level switch
            {
                1 => 0,
                2 => 100,
                3 => 300,
                4 => 600,
                5 => 1000,
                6 => 1500,
                7 => 2500,
                8 => 4000,
                9 => 6000,
                10 => 9000,
                11 => 13000,
                _ => 13000 + ((level - 11) * 5000)
            };
        }

        private string GetLevelName(int level)
        {
            return level switch
            {
                1 => "Novice",
                2 => "Beginner",
                3 => "Apprentice",
                4 => "Intermediate",
                5 => "Advanced",
                6 => "Expert",
                7 => "Master",
                8 => "Grandmaster",
                9 => "Champion",
                10 => "Legend",
                _ => $"Elite {level - 10}"
            };
        }

        public async Task ProcessProfileMilestoneAsync(string userId, string achievementKey, int completionPercentage)
        {
            try
            {
                _logger.LogInformation("Processing profile milestone for user {UserId}: {AchievementKey} ({Percentage}%)",
                    userId, achievementKey, completionPercentage);

                // Award points based on milestone
                int points = completionPercentage switch
                {
                    25 => 25,
                    50 => 50,
                    75 => 75,
                    100 => 100,
                    _ => 0
                };

                if (points > 0)
                {
                    await _pointsRepository.AwardPointsAsync(userId, points, $"ProfileMilestone_{completionPercentage}", achievementKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing profile milestone for user {UserId}", userId);
            }
        }

        public async Task ProcessChallengeProgressAsync(string userId, string actionType, int actionValue)
        {
            try
            {
                _logger.LogDebug("Processing challenge progress for user {UserId}: {ActionType} = {Value}",
                    userId, actionType, actionValue);

                // Update active user challenges
                var challenges = await _challengeRepository.GetUserActiveChallengesAsync(userId);
                foreach (var challenge in challenges)
                {
                    // Update progress based on action type
                    await _challengeRepository.UpdateChallengeProgressAsync(userId, challenge.Id, actionValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing challenge progress for user {UserId}", userId);
            }
        }

        public async Task ProcessPointsEarnedAsync(string userId, int points, string actionType, string referenceId = null)
        {
            try
            {
                _logger.LogInformation("Awarding {Points} points to user {UserId} for {ActionType}",
                    points, userId, actionType);

                await _pointsRepository.AwardPointsAsync(userId, points, actionType, referenceId);

                // Check for level up
                var stats = await GetUserStatsAsync(userId);
                await CheckAndAwardBadgesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing points for user {UserId}", userId);
            }
        }

        public async Task ProcessDocumentUploadAsync(string userId, int documentTypeId)
        {
            try
            {
                _logger.LogInformation("Processing document upload for user {UserId}, type {TypeId}",
                    userId, documentTypeId);

                // Award points for document upload
                await _pointsRepository.AwardPointsAsync(userId, 10, "DocumentUpload", documentTypeId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document upload for user {UserId}", userId);
            }
        }

        public async Task ProcessDocumentUploadAsync(string userId, string documentType)
        {
            try
            {
                _logger.LogInformation("Processing document upload for user {UserId}, type {Type}",
                    userId, documentType);

                // Award points for document upload
                await _pointsRepository.AwardPointsAsync(userId, 10, "DocumentUpload", documentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document upload for user {UserId}", userId);
            }
        }

        public async Task ProcessDocumentVerificationAsync(string userId, string documentTypeName)
        {
            try
            {
                _logger.LogInformation("Processing document verification for user {UserId}, type {Type}",
                    userId, documentTypeName);

                // Award points for verified document
                await _pointsRepository.AwardPointsAsync(userId, 25, "DocumentVerification", documentTypeName);

                // Check for achievements
                await CheckAndAwardBadgesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document verification for user {UserId}", userId);
            }
        }

        public async Task<UserStatsDto> GetUserStatsAsync(string userId)
        {
            return new UserStatsDto
            {
                TotalSurveys = 0,
                TotalPoints = 0,
                CurrentStreak = 0,
                LongestStreak = 0,
                AchievementsUnlocked = 0,
                ChallengesCompleted = 0
            };
        }
    }
}
