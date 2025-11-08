using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserProfileCompletionService : IUserProfileCompletionService
    {
        private readonly IProfileCompletionCalculator _completionCalculator;
        private readonly INotificationService _notificationService;
        private readonly IGamificationService _gamificationService;
        private readonly ILogger<UserProfileCompletionService> _logger;
        private readonly IMemoryCache _cache;

        public UserProfileCompletionService(
            IProfileCompletionCalculator completionCalculator,
            INotificationService notificationService,
            IGamificationService gamificationService,
            ILogger<UserProfileCompletionService> logger, IMemoryCache cache)
        {
            _completionCalculator = completionCalculator;
            _notificationService = notificationService;
            _gamificationService = gamificationService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<UserProfileCompletionDto> GetProfileCompletionAsync(string userId)
        {
            var cacheKey = $"profile_completion_{userId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _completionCalculator.CalculateCompletionAsync(userId);
            });
        }

        public async Task<List<string>> GetProfileCompletionSuggestionsAsync(string userId)
        {
            var completion = await GetProfileCompletionAsync(userId);

            return completion.NextSteps.Take(3).Select(step => step.Description).ToList();
        }

        public async Task<bool> IsEligibleForSurveysAsync(string userId)
        {
            var completion = await GetProfileCompletionAsync(userId);

            return completion.IsEligibleForSurveys;
        }

        private string GetMotivationalMessage(int completionPercentage)
        {
            return completionPercentage switch
            {
                < 25 => "Get started! Complete your profile to unlock surveys.",
                < 50 => "Great progress! You're halfway to unlocking surveys.",
                < 75 => "Almost there! Just a few more steps to access surveys.",
                < 100 => "So close! One final step and you can start earning.",
                _ => "Perfect! You can now access all available surveys."
            };
        }

        public async Task<ProfileUpdateResultDto> ProcessProfileUpdateAsync(string userId, string sectionUpdated)
        {
            _logger.LogInformation("Processing profile update for user {UserId}, section {Section}", userId, sectionUpdated);

            try
            {
                // Clear cache before getting updated completion
                InvalidateProfileCache(userId);

                var beforeCompletion = await GetProfileCompletionAsync(userId);

                // Wait a moment for any database updates to complete
                await Task.Delay(100);

                // Clear cache again to force fresh calculation
                InvalidateProfileCache(userId);

                var afterCompletion = await GetProfileCompletionAsync(userId);

                var result = new ProfileUpdateResultDto
                {
                    Success = true,
                    PreviousCompletion = beforeCompletion.OverallCompletionPercentage,
                    NewCompletion = afterCompletion.OverallCompletionPercentage,
                    SectionUpdated = sectionUpdated,
                    MilestonesAchieved = GetMilestonesAchieved(beforeCompletion, afterCompletion, sectionUpdated)
                };

                InvalidateSurveyCacheOnProfileUpdate(userId, beforeCompletion, afterCompletion);

                // Fire-and-forget operations for gamification and notifications
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessGamificationAsync(userId, sectionUpdated, result, beforeCompletion, afterCompletion);
                        await ProcessNotificationsAsync(userId, result, beforeCompletion, afterCompletion);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process profile update background operations for user {UserId}", userId);
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing profile update for user {UserId}, section {Section}", userId, sectionUpdated);
                return new ProfileUpdateResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to process profile update",
                    SectionUpdated = sectionUpdated
                };
            }
        }

        private void InvalidateProfileCache(string userId)
        {
            var cacheKey = $"profile_completion_{userId}";
            _cache.Remove(cacheKey);
        }

        private void InvalidateSurveyCacheOnProfileUpdate(string userId, UserProfileCompletionDto before, 
            UserProfileCompletionDto after)
        {
            // Only invalidate if survey eligibility changed
            var wasBefore = before.OverallCompletionPercentage >= 100;
            var isAfter = after.OverallCompletionPercentage >= 100;

            if (wasBefore != isAfter)
            {
                // Survey eligibility changed - invalidate cache
                _cache?.Remove($"matching_surveys_{userId}_70");
                _cache?.Remove($"profile_eligible_{userId}");

                _logger.LogInformation("Survey cache invalidated for user {UserId} - eligibility changed from {Before} to {After}",
                    userId, wasBefore, isAfter);
            }
        }

        private List<string> GetMilestonesAchieved(UserProfileCompletionDto before, UserProfileCompletionDto after, string sectionUpdated)
        {
            var milestones = new List<string>();

            // Section completion milestones
            var sectionCompletion = GetSectionCompletion(before, after, sectionUpdated);
            if (sectionCompletion.wasCompleted)
            {
                milestones.Add($"{sectionUpdated} Section Complete! 🎉");
            }

            // Overall completion milestones (25%, 50%, 75%, 100%)
            var overallMilestones = new[] { 25, 50, 75, 100 };
            foreach (var milestone in overallMilestones)
            {
                if (after.OverallCompletionPercentage >= milestone && before.OverallCompletionPercentage < milestone)
                {
                    var milestoneText = milestone switch
                    {
                        25 => "Profile Getting Started! 🚀",
                        50 => "Profile Halfway Complete! 📈",
                        75 => "Profile Almost Done! 💪",
                        100 => "Profile 100% Complete! 🎉 Surveys Unlocked!",
                        _ => $"{milestone}% Profile Complete!"
                    };
                    milestones.Add(milestoneText);
                }
            }

            // Survey eligibility milestone
            if (after.IsEligibleForSurveys && !before.IsEligibleForSurveys)
            {
                milestones.Add("Survey Access Unlocked! 🎯");
            }

            // Multi-section completion milestones
            var completedSections = GetCompletedSectionCount(after);
            var previousCompletedSections = GetCompletedSectionCount(before);

            if (completedSections > previousCompletedSections)
            {
                var multiSectionMilestone = completedSections switch
                {
                    2 => "Two Sections Complete! 🔥",
                    3 => "Three Sections Complete! ⭐",
                    4 => "All Sections Complete! 👑",
                    _ => null
                };

                if (multiSectionMilestone != null && !milestones.Any(m => m.Contains("100% Complete")))
                {
                    milestones.Add(multiSectionMilestone);
                }
            }

            return milestones;
        }

        private (bool wasCompleted, int previousPercentage, int newPercentage) GetSectionCompletion(
            UserProfileCompletionDto before, UserProfileCompletionDto after, string sectionUpdated)
        {
            var (previousPercentage, newPercentage) = sectionUpdated switch
            {
                "Demographics" => (before.Demographics.CompletionPercentage, after.Demographics.CompletionPercentage),
                "Banking" => (before.Banking.CompletionPercentage, after.Banking.CompletionPercentage),
                "Documents" => (before.Documents.CompletionPercentage, after.Documents.CompletionPercentage),
                "Interests" => (before.Interests.CompletionPercentage, after.Interests.CompletionPercentage),
                _ => (0, 0)
            };

            return (wasCompleted: previousPercentage < 25 && newPercentage >= 25, previousPercentage, newPercentage);
        }

        private int GetCompletedSectionCount(UserProfileCompletionDto completion)
        {
            var sections = new[]
            {
        completion.Demographics.CompletionPercentage,
        completion.Banking.CompletionPercentage,
        completion.Documents.CompletionPercentage,
        completion.Interests.CompletionPercentage
    };

            return sections.Count(percentage => percentage >= 25);
        }

        private async Task ProcessGamificationAsync(string userId, string sectionUpdated, ProfileUpdateResultDto result,
            UserProfileCompletionDto before, UserProfileCompletionDto after)
        {
            // Award points for section completion
            var sectionCompletion = GetSectionCompletion(before, after, sectionUpdated);
            if (sectionCompletion.wasCompleted)
            {
                var points = sectionUpdated switch
                {
                    "Demographics" => 50,
                    "Banking" => 75,
                    "Documents" => 100,
                    "Interests" => 25,
                    _ => 25
                };

                await _gamificationService.ProcessPointsEarnedAsync(userId, points, "ProfileSectionComplete", sectionUpdated);
            }

            // Process overall milestones for additional rewards
            foreach (var milestone in result.MilestonesAchieved)
            {
                if (milestone.Contains("25%") || milestone.Contains("50%") || milestone.Contains("75%") || milestone.Contains("100%"))
                {
                    var milestonePercentage = ExtractPercentageFromMilestone(milestone);
                    await _gamificationService.ProcessProfileMilestoneAsync(userId, milestone, milestonePercentage);
                }
            }

            // Special processing for survey unlock
            if (after.IsEligibleForSurveys && !before.IsEligibleForSurveys)
            {
                await _gamificationService.ProcessPointsEarnedAsync(userId, 200, "ProfileComplete", "SurveyEligibility");
            }
        }

        private async Task ProcessNotificationsAsync(string userId, ProfileUpdateResultDto result,
            UserProfileCompletionDto before, UserProfileCompletionDto after)
        {
            // Send section completion notification
            var sectionCompletion = GetSectionCompletion(before, after, result.SectionUpdated);
            if (sectionCompletion.wasCompleted)
            {
                await _notificationService.SendSectionCompletionNotificationAsync(userId, result.SectionUpdated, 25);
            }

            // Send milestone notifications
            foreach (var milestone in result.MilestonesAchieved)
            {
                if (milestone.Contains("Profile") && (milestone.Contains("25%") || milestone.Contains("50%") ||
                    milestone.Contains("75%") || milestone.Contains("100%")))
                {
                    var percentage = ExtractPercentageFromMilestone(milestone);
                    await _notificationService.SendProfileMilestoneNotificationAsync(userId, milestone, percentage);
                }
            }

            // Send survey eligibility notification
            if (after.IsEligibleForSurveys && !before.IsEligibleForSurveys)
            {
                await _notificationService.SendSurveyEligibilityUnlockedNotificationAsync(userId);
            }

            // Send encouragement for partial progress
            if (result.HasImprovement && !result.MilestonesAchieved.Any(m => m.Contains("Complete")))
            {
                var nextSection = GetNextPrioritySection(after);
                if (!string.IsNullOrEmpty(nextSection))
                {
                    await _notificationService.SendProfileBoostNotificationAsync(userId, result.SectionUpdated, result.ImprovementPercentage);
                }
            }
        }

        private int ExtractPercentageFromMilestone(string milestone)
        {
            if (milestone.Contains("100%")) return 100;
            if (milestone.Contains("75%")) return 75;
            if (milestone.Contains("50%")) return 50;
            if (milestone.Contains("25%")) return 25;
            return 0;
        }

        private string GetNextPrioritySection(UserProfileCompletionDto completion)
        {
            // Return the highest priority incomplete section
            var incompleteSections = new[]
            {
                (completion.Documents, "Documents", 1),     // Highest priority
                (completion.Demographics, "Demographics", 2),
                (completion.Banking, "Banking", 3),
                (completion.Interests, "Interests", 4)      // Lowest priority
            }
            .Where(s => s.Item1.CompletionPercentage < 25)
            .OrderBy(s => s.Item3)
            .FirstOrDefault();

            return incompleteSections.Item2;
        }
    }
}
