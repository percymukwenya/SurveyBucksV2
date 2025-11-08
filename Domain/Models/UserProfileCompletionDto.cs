using Domain.Models.Enums;

namespace Domain.Models
{
    public class UserProfileCompletionDto
    {
        public string UserId { get; set; }
        public int OverallCompletionPercentage { get; set; }
        public ProfileSectionCompletionDto Demographics { get; set; }
        public ProfileSectionCompletionDto Documents { get; set; }
        public ProfileSectionCompletionDto Banking { get; set; }
        public ProfileSectionCompletionDto Interests { get; set; }
        public List<ProfileNextStepDto> NextSteps { get; set; } = new();
        public bool IsEligibleForSurveys { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

        // Helper properties for UI
        public string CompletionStatusText => OverallCompletionPercentage switch
        {
            100 => "Complete! 🎉",
            >= 80 => "Almost there! 💪",
            >= 50 => "Good progress! 📈",
            >= 25 => "Getting started! 🚀",
            _ => "Let's begin! ✨"
        };

        public string EligibilityStatusText => IsEligibleForSurveys
            ? "Ready for surveys! 🎯"
            : $"Complete {100 - OverallCompletionPercentage}% more to unlock surveys";

        public List<ProfileSectionCompletionDto> GetSectionsByPriority()
        {
            return new[] { Demographics, Documents, Banking, Interests }
                .OrderBy(s => s.CompletionPercentage)
                .ToList();
        }
    }

    public class ProfileSectionCompletionDto
    {
        public string SectionName { get; set; }
        public int Weight { get; set; } // 25% each
        public int CompletionPercentage { get; set; }
        public List<string> CompletedFields { get; set; } = new();
        public List<string> MissingFields { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();

        // Helper properties for UI
        public string StatusIcon => CompletionPercentage switch
        {
            100 => "✅",
            >= 75 => "🔶",
            >= 50 => "🔸",
            >= 25 => "🔹",
            _ => "⚪"
        };

        public string StatusText => CompletionPercentage switch
        {
            100 => "Complete",
            >= 75 => "Almost Done",
            >= 50 => "In Progress",
            >= 25 => "Started",
            _ => "Not Started"
        };

        public ProfileSectionStatus Status => CompletionPercentage switch
        {
            100 => ProfileSectionStatus.Complete,
            >= 75 => ProfileSectionStatus.AlmostComplete,
            >= 50 => ProfileSectionStatus.InProgress,
            >= 25 => ProfileSectionStatus.Started,
            _ => ProfileSectionStatus.NotStarted
        };
    }

    public class ProfileNextStepDto
    {
        public string Section { get; set; }
        public ProfileStepPriority Priority { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> MissingItems { get; set; } = new();
        public string ImpactDescription { get; set; }
        public int EstimatedTimeMinutes { get; set; }

        // Helper properties for UI
        public string PriorityIcon => Priority switch
        {
            ProfileStepPriority.Critical => "🔴",
            ProfileStepPriority.High => "🟠",
            ProfileStepPriority.Medium => "🟡",
            ProfileStepPriority.Low => "🟢",
            _ => "⚪"
        };

        public string PriorityText => Priority switch
        {
            ProfileStepPriority.Critical => "Critical",
            ProfileStepPriority.High => "High Priority",
            ProfileStepPriority.Medium => "Medium Priority",
            ProfileStepPriority.Low => "Low Priority",
            _ => "Optional"
        };

        public string EstimatedTimeText => EstimatedTimeMinutes switch
        {
            <= 2 => "2 minutes",
            <= 5 => "5 minutes",
            <= 10 => "10 minutes",
            <= 15 => "15 minutes",
            _ => "20+ minutes"
        };
    }

    public class ProfileUpdateResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int PreviousCompletion { get; set; }
        public int NewCompletion { get; set; }
        public string SectionUpdated { get; set; }
        public List<string> MilestonesAchieved { get; set; } = new();

        // Helper properties
        public int ImprovementPercentage => NewCompletion - PreviousCompletion;
        public bool HasImprovement => ImprovementPercentage > 0;
        public bool HasMilestones => MilestonesAchieved.Any();
    }
}
