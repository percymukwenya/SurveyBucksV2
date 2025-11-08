using Application.Services.Email;
using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IPushNotificationService _pushNotificationService;

        public NotificationService(INotificationRepository notificationRepository, IEmailService emailService, 
            IPushNotificationService pushNotificationService)
        {
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _pushNotificationService = pushNotificationService;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            return await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly);
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
        {
            return await _notificationRepository.MarkNotificationAsReadAsync(notificationId, userId);
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            return await _notificationRepository.MarkAllNotificationsAsReadAsync(userId);
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _notificationRepository.GetUnreadNotificationCountAsync(userId);
        }

        public async Task<bool> CreateNotificationAsync(string userId, string title, string message, string notificationType, string referenceId = null, string referenceType = null, string deepLink = null)
        {
            // Validate notification
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Notification title cannot be empty");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Notification message cannot be empty");
            }

            return await _notificationRepository.CreateNotificationAsync(userId, title, message, notificationType, referenceId, referenceType, deepLink);
        }

        public async Task<bool> SendSystemNotificationToAllUsersAsync(string title, string message, string notificationType)
        {
            // Validate notification
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Notification title cannot be empty");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Notification message cannot be empty");
            }

            return await _notificationRepository.SendSystemNotificationToAllUsersAsync(title, message, notificationType);
        }

        public async Task<bool> SendNotificationToUserGroupAsync(IEnumerable<string> userIds, string title, string message, string notificationType)
        {
            // Validate notification
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Notification title cannot be empty");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Notification message cannot be empty");
            }

            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException("User IDs list cannot be empty");
            }

            return await _notificationRepository.SendNotificationToUserGroupAsync(userIds, title, message, notificationType);
        }

        public async Task SendRewardRedeemedNotificationAsync(string userId, string rewardName, string redemptionCode)
        {
            // In-app notification
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Reward Redeemed! 🎁",
                $"You've successfully redeemed '{rewardName}'! Your redemption code is: {redemptionCode}",
                "RewardRedemption",
                redemptionCode,
                "Reward");

            // Email notification (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendRewardRedemptionEmailAsync(userId, rewardName, redemptionCode);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the main operation
                    Console.WriteLine($"Failed to send reward redemption email: {ex.Message}");
                }
            });

            // Push notification
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Reward Redeemed!", $"Your {rewardName} is ready!"));
        }

        public async Task SendRewardClaimedNotificationAsync(string userId, string rewardName)
        {
            // In-app notification
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Reward Claimed Successfully! ✅",
                $"You've successfully claimed your '{rewardName}' reward. It's now being processed for delivery.",
                "RewardClaim",
                rewardName,
                "Reward");

            // Email confirmation (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendRewardClaimConfirmationEmailAsync(userId, rewardName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send reward claim confirmation email: {ex.Message}");
                }
            });
        }

        public async Task SendRewardDeliveryNotificationAsync(string userId, string rewardName, string trackingInfo = null)
        {
            var message = trackingInfo != null
                ? $"Your '{rewardName}' reward has been shipped! Tracking: {trackingInfo}"
                : $"Your '{rewardName}' reward is being processed for delivery.";

            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Reward Shipped! 📦",
                message,
                "RewardDelivery",
                trackingInfo ?? rewardName,
                "Reward");
        }

        public async Task SendLowPointsWarningAsync(string userId, string rewardName, int pointsNeeded, int pointsAvailable)
        {
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Not Enough Points 💰",
                $"You need {pointsNeeded} points for '{rewardName}' but only have {pointsAvailable}. Complete more surveys to earn points!",
                "InsufficientPoints",
                rewardName,
                "Reward");
        }

        public Task SendEnrollmentNotificationAsync(string userId, int surveyId)
        {
            throw new NotImplementedException();
        }

        public Task SendCompletionNotificationAsync(string userId, int surveyId)
        {
            throw new NotImplementedException();
        }

        public async Task SendAchievementNotificationAsync(string userId, string achievementKey)
        {
            var achievementName = GetAchievementDisplayName(achievementKey);
            var achievementEmoji = GetAchievementEmoji(achievementKey);

            await _notificationRepository.CreateNotificationAsync(
                userId,
                $"Achievement Unlocked! {achievementEmoji}",
                $"Congratulations! You've unlocked the '{achievementName}' achievement!",
                "Achievement",
                achievementKey,
                "Achievement");

            // Push notification for achievements (high priority)
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Achievement Unlocked!", $"You earned: {achievementName}"));
        }

        public async Task SendLevelUpNotificationAsync(string userId, int newLevel)
        {
            var levelName = GetLevelDisplayName(newLevel);

            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Level Up! 🚀",
                $"Amazing! You've reached Level {newLevel} ({levelName})! Keep up the great work!",
                "LevelUp",
                newLevel.ToString(),
                "Level");

            // Special celebration for major levels
            if (newLevel % 5 == 0 || newLevel >= 10)
            {
                _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Major Level Up!", $"Welcome to Level {newLevel} - {levelName}!"));
                _ = Task.Run(() => _emailService.SendLevelUpCelebrationEmailAsync(userId, newLevel, levelName));
            }
        }

        public async Task SendFirstRedemptionBonusNotificationAsync(string userId)
        {
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "First Redemption Bonus! 🎁",
                "Welcome to the rewards family! You've earned 50 bonus points for your first redemption!",
                "FirstRedemptionBonus",
                "50",
                "Reward");

            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Bonus Points!", "50 bonus points for your first redemption!"));
        }

        public async Task SendChallengeCompletedNotificationAsync(string userId, string challengeName, int pointsAwarded)
        {
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Challenge Completed! 🏆",
                $"Excellent work! You've completed the '{challengeName}' challenge and earned {pointsAwarded} points!",
                "ChallengeCompleted",
                challengeName,
                "Challenge");

            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Challenge Complete!", $"You conquered: {challengeName}"));
        }

        public async Task SendStreakNotificationAsync(string userId, int streakDays)
        {
            var message = streakDays switch
            {
                3 => "You're on a 3-day streak! Keep it going! 🔥",
                7 => "Amazing! 7 days in a row! You're unstoppable! 🌟",
                14 => "Incredible! 2 weeks straight! You're a legend! ⭐",
                30 => "PHENOMENAL! 30 days in a row! You're absolutely amazing! 👑",
                _ => $"Fantastic! {streakDays} days in a row! Keep the momentum! 🔥"
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                $"Login Streak: {streakDays} Days! 🔥",
                message,
                "LoginStreak",
                streakDays.ToString(),
                "Engagement");

            // Special celebration for major streaks
            if (streakDays >= 7)
            {
                _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Streak Milestone!", $"{streakDays} days in a row! Amazing!"));
            }
        }

        public async Task SendMilestoneNotificationAsync(string userId, string milestoneType, int milestoneValue, int bonusPoints)
        {
            var (title, message, emoji) = milestoneType switch
            {
                "Points" => ("Points Milestone! 💰", $"Incredible! You've earned {milestoneValue:N0} total points! Here's {bonusPoints} bonus points!", "💰"),
                "Surveys" => ("Survey Milestone! 📋", $"Amazing! You've completed {milestoneValue} surveys! Here's {bonusPoints} bonus points!", "📋"),
                "Spending" => ("Spending Milestone! 💸", $"Wow! You've spent {milestoneValue:N0} points on rewards! Here's {bonusPoints} bonus points!", "💸"),
                _ => ("Milestone Reached! 🎯", $"You've reached a milestone! Here's {bonusPoints} bonus points!", "🎯")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "Milestone",
                milestoneValue.ToString(),
                "Achievement");

            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Milestone Bonus!", $"{bonusPoints} bonus points earned!"));
        }

        public async Task SendEngagementEncouragementAsync(string userId, string encouragementType)
        {
            var (title, message) = encouragementType switch
            {
                "ReturnAfterBreak" => ("Welcome Back! 👋", "We missed you! Complete a survey today to get back on track!"),
                "CloseToLevel" => ("Almost There! 🎯", "You're so close to leveling up! Just a few more points to go!"),
                "NewChallenges" => ("New Challenges Available! 🏆", "Fresh challenges are waiting for you! Ready to take them on?"),
                "WeeklyGoal" => ("Weekly Goal Reminder 📅", "You're doing great this week! Keep up the momentum!"),
                _ => ("Keep Going! 💪", "You're doing amazing! Every survey gets you closer to your goals!")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "Encouragement",
                encouragementType,
                "Engagement");
        }

        public async Task SendLeaderboardUpdateAsync(string userId, string leaderboardName, int currentRank, int? previousRank)
        {
            string message;
            string emoji;

            if (!previousRank.HasValue)
            {
                message = $"You've entered the {leaderboardName} leaderboard at rank #{currentRank}! Great start!";
                emoji = "📈";
            }
            else if (currentRank < previousRank.Value)
            {
                var improvement = previousRank.Value - currentRank;
                message = $"You've climbed {improvement} positions to rank #{currentRank} on the {leaderboardName} leaderboard! Keep climbing!";
                emoji = "⬆️";
            }
            else if (currentRank > previousRank.Value)
            {
                message = $"You're now at rank #{currentRank} on the {leaderboardName} leaderboard. Time to make a comeback!";
                emoji = "📊";
            }
            else
            {
                message = $"You're holding strong at rank #{currentRank} on the {leaderboardName} leaderboard!";
                emoji = "🔒";
            }

            await _notificationRepository.CreateNotificationAsync(
                userId,
                $"Leaderboard Update {emoji}",
                message,
                "LeaderboardUpdate",
                currentRank.ToString(),
                "Leaderboard");

            // Push notification for significant rank changes
            if (!previousRank.HasValue || Math.Abs(currentRank - previousRank.Value) >= 3)
            {
                _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Leaderboard Update", $"Rank #{currentRank} on {leaderboardName}"));
            }
        }

        // HELPER METHODS - Achievement and level display names
        private string GetAchievementDisplayName(string achievementKey)
        {
            var achievements = new Dictionary<string, string>
            {
                // Login achievements
                ["ConsecutiveLogins3"] = "Early Bird",
                ["ConsecutiveLogins7"] = "Week Warrior",
                ["ConsecutiveLogins14"] = "Fortnight Fighter",
                ["ConsecutiveLogins30"] = "Monthly Master",
                ["ConsecutiveLogins60"] = "Dedication King",
                ["ConsecutiveLogins100"] = "Loyalty Legend",
                ["MaxStreak7"] = "Streak Starter",
                ["MaxStreak30"] = "Streak Specialist",
                ["MaxStreak100"] = "Streak Supreme",

                // Survey achievements
                ["FirstEnrollment"] = "First Steps",
                ["SurveyExplorer"] = "Survey Explorer",
                ["SurveySeeker"] = "Survey Seeker",
                ["SurveyHunter"] = "Survey Hunter",
                ["SurveyCollector"] = "Survey Collector",
                ["SurveyMaster"] = "Survey Master",
                ["FirstCompletion"] = "Completion Champion",
                ["SurveyNovice"] = "Survey Novice",
                ["SurveyAdept"] = "Survey Adept",
                ["SurveyExpert"] = "Survey Expert",
                ["SurveyPro"] = "Survey Professional",
                ["SurveyLegend"] = "Survey Legend",

                // Reward achievements
                ["FirstRedemption"] = "First Purchase",
                ["ShopperNovice"] = "Shopper Novice",
                ["ShopperExpert"] = "Shopper Expert",
                ["ShopperMaster"] = "Shopper Master",
                ["ShopperLegend"] = "Shopping Legend",
                ["FirstClaim"] = "First Claim",
                ["RewardCollector"] = "Reward Collector",

                // Challenge achievements
                ["FirstChallenge"] = "Challenge Accepted",
                ["ChallengeChampion"] = "Challenge Champion",

                // Progress achievements
                ["Progress25Percent"] = "Quarter Master",
                ["Progress50Percent"] = "Halfway Hero",
                ["Progress75Percent"] = "Almost There",

                // Spending achievements
                ["Spender100"] = "Spender Starter",
                ["Spender500"] = "Smart Spender",
                ["Spender1000"] = "Big Spender",
                ["Spender5000"] = "Mega Spender",
                ["Spender10000"] = "Ultimate Spender",

                // Engagement achievements
                ["ActiveUser"] = "Active User",
                ["SuperUser"] = "Super User"
            };

            return achievements.TryGetValue(achievementKey, out var name) ? name : achievementKey;
        }

        private string GetAchievementEmoji(string achievementKey)
        {
            return achievementKey switch
            {
                var key when key.Contains("Login") || key.Contains("Streak") => "🔥",
                var key when key.Contains("Survey") => "📋",
                var key when key.Contains("Shopper") || key.Contains("Redemption") || key.Contains("Claim") => "🛒",
                var key when key.Contains("Challenge") => "🏆",
                var key when key.Contains("Progress") => "📈",
                var key when key.Contains("Spender") => "💰",
                var key when key.Contains("User") => "⭐",
                _ => "🏅"
            };
        }

        private string GetLevelDisplayName(int level)
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

        public async Task SendProfileMilestoneNotificationAsync(string userId, string milestone, int completionPercentage)
        {
            var (title, message, emoji) = milestone switch
            {
                "25% Profile Complete" => ("Great Start! 🚀", "You're 25% done with your profile! Keep going to unlock more survey opportunities.", "🚀"),
                "50% Profile Complete" => ("Halfway There! 🎯", "Amazing progress! You're 50% complete. Just a bit more to unlock surveys!", "🎯"),
                "75% Profile Complete" => ("Almost Ready! 💪", "Fantastic! You're 75% complete. You're so close to accessing all surveys!", "💪"),
                "100% Profile Complete" => ("Profile Complete! 🎉", "Congratulations! Your profile is 100% complete. You now have access to all surveys and features!", "🎉"),
                _ => ("Profile Milestone! ⭐", $"You've reached {milestone}! Keep up the great work!", "⭐")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                $"{title} {emoji}",
                message,
                "ProfileMilestone",
                completionPercentage.ToString(),
                "Profile");

            // Push notification for major milestones
            if (completionPercentage >= 50)
            {
                _ = Task.Run(() => _pushNotificationService.SendAsync(userId, title, $"{completionPercentage}% complete!"));
            }

            // Email celebration for 100% completion
            if (completionPercentage == 100)
            {
                _ = Task.Run(() => _emailService.SendProfileCompletionCelebrationAsync(userId));
            }
        }

        public async Task SendProfileCompletionNotificationAsync(string userId)
        {
            await _notificationRepository.CreateNotificationAsync(
            userId,
            "Profile Complete! Welcome to Full Access! 🎉",
            "Your profile is now 100% complete! You can access all surveys, redeem rewards, and enjoy the full experience. Well done!",
            "ProfileComplete",
            "100",
            "Profile");

            // Special celebration notifications
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Profile Complete! 🎉", "You now have full access to everything!"));
            _ = Task.Run(() => _emailService.SendProfileCompletionCelebrationAsync(userId));
        }

        public async Task SendProfileNearCompletionNotificationAsync(string userId, int percentageRemaining)
        {
            var message = percentageRemaining switch
            {
                <= 5 => "You're so close! Just one more step to complete your profile and unlock everything!",
                <= 10 => $"Only {percentageRemaining}% left! You're almost there - finish strong!",
                <= 25 => $"Great progress! Just {percentageRemaining}% more to complete your profile and access all surveys!",
                _ => $"Keep going! You need {percentageRemaining}% more to complete your profile."
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Almost There! 🔥",
                message,
                "ProfileNearCompletion",
                percentageRemaining.ToString(),
                "Profile");
        }

        public async Task SendProfileReminderNotificationAsync(string userId, string sectionName)
        {
            var (title, message) = sectionName switch
            {
                "Demographics" => ("Complete Your Demographics 👤", "Add your basic information to help us match you with relevant surveys!"),
                "Documents" => ("Upload Identity Documents 📄", "Verify your identity to unlock rewards and payments!"),
                "Banking" => ("Add Banking Details 💳", "Set up your payment method to receive survey rewards!"),
                "Interests" => ("Share Your Interests 🎯", "Tell us what you're interested in for better survey matching!"),
                _ => ("Complete Your Profile ✨", "Finish setting up your profile to access more features!")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "ProfileReminder",
                sectionName,
                "Profile");
        }

        public async Task SendDetailedMilestoneNotificationAsync(string userId, string milestone, int percentage, int sectionsComplete)
        {
            var (title, message, emoji) = percentage switch
            {
                25 => ("Quarter Way There! 🚀",
                    $"Great start! You've completed {sectionsComplete} section(s). Your profile is {percentage}% complete!", "🚀"),

                50 => ("Halfway Point! 📈",
                    $"Fantastic progress! You've completed {sectionsComplete} sections. Your profile is {percentage}% complete!", "📈"),

                75 => ("Almost Ready! 💪",
                    $"You're so close! {sectionsComplete} sections done, just one more to unlock surveys!", "💪"),

                100 => ("Profile Complete! 🎉",
                    $"Congratulations! All {sectionsComplete} sections complete. You can now participate in surveys!", "🎉"),

                _ => ("Progress Made! ⭐",
                    $"Nice work! Your profile is {percentage}% complete with {sectionsComplete} sections done.", "⭐")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                $"{title} {emoji}",
                message,
                "ProfileMilestone",
                percentage.ToString(),
                "Profile");

            // Push notification for major milestones
            if (percentage >= 50)
            {
                _ = Task.Run(() => _pushNotificationService.SendAsync(userId, title, $"{percentage}% profile complete!"));
            }

            // Email celebration for 100% completion
            if (percentage == 100)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendProfileCompletionCelebrationAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send profile completion celebration email: {ex.Message}");
                    }
                });
            }
        }

        public async Task SendSurveyEligibilityUnlockedNotificationAsync(string userId)
        {
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Surveys Unlocked! 🎯",
                "Congratulations! Your profile is now complete enough to participate in surveys. Start earning points today!",
                "SurveyEligibility",
                "Unlocked",
                "Profile");

            // High-priority push notification
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Surveys Unlocked! 🎯", "You can now participate in surveys!"));
        }

        public async Task SendProfileBoostNotificationAsync(string userId, string actionType, int pointsEarned)
        {
            var (title, message) = actionType switch
            {
                "Demographics" => ("Profile Boost! 💰",
                    $"Nice work! You earned {pointsEarned} points for updating your personal information. Keep building your profile!"),

                "Documents" => ("Verification Bonus! 💰",
                    $"Great job! You earned {pointsEarned} points for uploading your documents. Verification unlocks more opportunities!"),

                "Banking" => ("Payment Setup Bonus! 💰",
                    $"Excellent! You earned {pointsEarned} points for adding your banking details. You're ready to get paid!"),

                "Interests" => ("Interest Bonus! 💰",
                    $"Awesome! You earned {pointsEarned} points for sharing your interests. This helps us find perfect surveys for you!"),

                _ => ("Profile Update Bonus! 💰",
                    $"You earned {pointsEarned} points for updating your profile! Every step gets you closer to survey eligibility.")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "ProfileBonus",
                pointsEarned.ToString(),
                "Points");

            // Send push notification for points earned
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, "Points Earned! 💰", $"+{pointsEarned} points for profile update"));
        }

        public async Task SendProfileTipNotificationAsync(string userId, string tipCategory)
        {
            var (title, message) = tipCategory switch
            {
                "Demographics" => ("Tip: Complete Demographics 💡", "Adding your age, location, and income helps us find surveys that pay more!"),
                "Documents" => ("Tip: Verify Your Identity 💡", "Verified users get access to premium surveys with higher rewards!"),
                "Banking" => ("Tip: Add Payment Method 💡", "Set up banking details to receive your survey earnings directly!"),
                "Interests" => ("Tip: Share More Interests 💡", "The more interests you add, the better we can match surveys you'll enjoy!"),
                "General" => ("Tip: Complete Your Profile 💡", "Users with complete profiles earn 3x more than those with incomplete profiles!"),
                _ => ("Profile Tip 💡", "Complete more of your profile to unlock better opportunities!")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "ProfileTip",
                tipCategory,
                "Profile");
        }

        // PROFILE-SPECIFIC ENGAGEMENT NOTIFICATIONS
        public async Task SendProfileEngagementAsync(string userId, string engagementType, Dictionary<string, object> parameters = null)
        {
            var (title, message) = engagementType switch
            {
                "WeeklyProfileGoal" => ("Weekly Goal: Complete Your Profile 📅",
                    "This week, try to get your profile to 100%! Complete users earn more rewards."),

                "ProfileInactiveReminder" => ("Don't Forget Your Profile! 👋",
                    "You started setting up your profile but haven't finished. Just a few more steps to unlock surveys!"),

                "ProfileComparisonMotivation" => ("You're Doing Great! 📊",
                    $"Your profile is {parameters?.GetValueOrDefault("completionPercentage", 0)}% complete - that's better than {parameters?.GetValueOrDefault("percentileBetter", 0)}% of users!"),

                "MissingHighValueSection" => ("Quick Win Opportunity! 🎯",
                    $"Adding {parameters?.GetValueOrDefault("sectionName", "your information")} would unlock {parameters?.GetValueOrDefault("potentialSurveys", 0)} more surveys!"),

                _ => ("Profile Update Available 📝", "There are new ways to improve your profile and earn more!")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "ProfileEngagement",
                engagementType,
                "Profile");
        }

        public Task SendDocumentUploadedNotificationAsync(string userId, string documentTypeName)
        {
            throw new NotImplementedException();
        }

        public Task SendDocumentApprovedNotificationAsync(string userId, string documentTypeName)
        {
            throw new NotImplementedException();
        }

        public Task SendDocumentRejectedNotificationAsync(string userId, string documentTypeName, string reason)
        {
            throw new NotImplementedException();
        }

        public Task SendDocumentDeletedNotificationAsync(string userId, string documentTypeName)
        {
            throw new NotImplementedException();
        }

        public async Task SendSectionCompletionNotificationAsync(string userId, string sectionName, int sectionPercentage)
        {
            var (emoji, celebration) = sectionPercentage switch
            {
                25 => ("✅", "completed"),
                >= 75 => ("🔶", "almost finished with"),
                >= 50 => ("🔸", "making great progress on"),
                _ => ("🔹", "started working on")
            };

            var (title, message) = sectionName switch
            {
                "Demographics" => ("Personal Info Complete! ✅",
                    "Great! Your demographic information is now complete. This unlocks better survey matching!"),

                "Banking" => ("Payment Method Added! ✅",
                    "Excellent! Your banking details are verified. You can now receive payments for completed surveys!"),

                "Documents" => ("Identity Verified! ✅",
                    "Perfect! Your identity document has been approved. You're now eligible for premium surveys!"),

                "Interests" => ("Interests Added! ✅",
                    "Awesome! You've added enough interests. This helps us find surveys you'll enjoy!"),

                _ => ($"Section Progress {emoji}",
                    $"You've {celebration} your {sectionName} section ({sectionPercentage}% complete)!")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                message,
                "SectionProgress",
                $"{sectionName}_{sectionPercentage}",
                "Profile");

            // Send push notification for section completion
            _ = Task.Run(() => _pushNotificationService.SendAsync(userId, title, $"{sectionName} section complete!"));
        }

        public async Task SendProfileNextStepSuggestionAsync(string userId, string nextSection, int timeEstimate)
        {
            var (title, message, benefit) = nextSection switch
            {
                "Demographics" => ("Quick Win: Add Personal Info! ⚡",
                    $"Spend {timeEstimate} minutes completing your demographics",
                    "unlock survey matching"),

                "Documents" => ("Next: Verify Your Identity! 📄",
                    $"Take {timeEstimate} minutes to upload your ID",
                    "access premium surveys"),

                "Banking" => ("Next: Add Payment Method! 💳",
                    $"Spend {timeEstimate} minutes setting up banking",
                    "receive survey payments"),

                "Interests" => ("Final Step: Add Interests! 🎯",
                    $"Just {timeEstimate} minutes to add your interests",
                    "get better survey matches"),

                _ => ("Keep Building Your Profile! ✨",
                    $"Continue with the {nextSection} section",
                    "complete your profile")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                $"{message} to {benefit}!",
                "ProfileSuggestion",
                nextSection,
                "Profile");
        }

        public async Task SendWeeklyProfileGoalAsync(string userId, int currentCompletion, string focusSection)
        {
            var goalMessage = currentCompletion switch
            {
                >= 75 => "This week's goal: Complete your profile and start earning! 🎯",
                >= 50 => $"This week's goal: Finish the {focusSection} section! 📈",
                >= 25 => $"This week's goal: Complete {focusSection} and one more section! 🚀",
                _ => "This week's goal: Get your profile to 50%! ✨"
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Weekly Profile Goal 📅",
                goalMessage,
                "WeeklyGoal",
                focusSection,
                "Profile");
        }

        public async Task SendProfileSectionEncouragementAsync(string userId, string sectionName, int currentProgress)
        {
            var (title, message, encouragement) = sectionName switch
            {
                "Demographics" => ("Complete Your Profile! 👤",
                    "Add your personal information to unlock survey matching!",
                    "demographic details help us find surveys that match your profile"),

                "Documents" => ("Verify Your Identity! 📄",
                    "Upload your ID to unlock premium surveys and payments!",
                    "identity verification gives you access to higher-paying opportunities"),

                "Banking" => ("Add Payment Method! 💳",
                    "Set up your banking details to receive survey earnings!",
                    "payment setup means you can cash out your rewards"),

                "Interests" => ("Share Your Interests! 🎯",
                    "Tell us what you're passionate about for better survey matches!",
                    "interests help us find surveys you'll actually enjoy"),

                _ => ("Keep Going! ✨",
                    $"You're making progress on your {sectionName} section!",
                    "every step brings you closer to survey eligibility")
            };

            await _notificationRepository.CreateNotificationAsync(
                userId,
                title,
                $"{message} Your {encouragement}.",
                "ProfileEncouragement",
                sectionName,
                "Profile");
        }
    }
}
