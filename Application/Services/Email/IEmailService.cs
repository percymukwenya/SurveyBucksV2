using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendCompletionEmailAsync(string userId, string surveyName);
        Task SendRewardRedemptionEmailAsync(string userId, string rewardName, string redemptionCode);
        Task SendRewardClaimConfirmationEmailAsync(string userId, string rewardName);
        Task SendLevelUpCelebrationEmailAsync(string userId, int newLevel, string levelName);

        Task SendProfileCompletionCelebrationAsync(string userId);
        Task SendProfileMilestoneEmailAsync(string userId, string milestone, int completionPercentage);
        Task SendProfileReminderEmailAsync(string userId, List<string> missingSections);
        Task SendSurveyEligibilityEmailAsync(string userId);

        Task SendPasswordChangedNotificationAsync(string email, string firstName);
        Task SendAccountLockedEmailAsync(string email, string firstName, int lockoutMinutes);
        Task SendWelcomeEmailAsync(string userId);
        Task SendPasswordResetConfirmationAsync(string email, string firstName);
        Task SendPasswordResetAsync(string email, string firstName, string resetLink);
        Task SendEmailConfirmationAsync(string email, string firstName, string confirmationLink);
    }
}
