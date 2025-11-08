namespace Domain.Models
{
    public class ProfileCompletenessResult
    {
        public bool IsComplete { get; set; }
        public int TotalPoints { get; set; }
        public bool DemographicsComplete { get; set; }
        public bool BankingComplete { get; set; }
        public bool DocumentsComplete { get; set; }
        public bool InterestsComplete { get; set; }

        // Additional context for debugging
        public int InterestCount { get; set; }
        public int VerifiedBankingCount { get; set; }
        public int ApprovedDocsCount { get; set; }
    }

    public class DetailedProfileCompletionDto
    {
        public bool IsEligibleForSurveys { get; set; }
        public int OverallCompletionPercentage { get; set; }
        public SectionCompletionDto Demographics { get; set; }
        public SectionCompletionDto Banking { get; set; }
        public SectionCompletionDto Documents { get; set; }
        public SectionCompletionDto Interests { get; set; }

        public List<string> GetBlockingFactors()
        {
            var factors = new List<string>();

            if (!Demographics.IsComplete)
                factors.Add("Complete personal information (gender, age, location, income)");

            if (!Documents.IsComplete)
                factors.Add("Upload and verify identity document");

            if (!Banking.IsComplete)
                factors.Add("Add and verify banking details");

            if (!Interests.IsComplete)
                factors.Add($"Add interests (current: {Interests.CurrentCount}, required: {Interests.RequiredCount})");

            return factors;
        }

        public string GetMotivationalMessage()
        {
            return OverallCompletionPercentage switch
            {
                0 => "Let's get started! Complete your profile to unlock surveys.",
                25 => "Great start! You're 25% complete - keep going!",
                50 => "Halfway there! You're doing amazing!",
                75 => "So close! Just one more section to unlock surveys!",
                100 => "Perfect! You can now access all available surveys!",
                _ => $"You're {OverallCompletionPercentage}% complete - almost there!"
            };
        }
    }

    public class SectionCompletionDto
    {
        public bool IsComplete { get; set; }
        public int CompletionPercentage { get; set; }
        public List<string> RequiredItems { get; set; } = new();
        public int CurrentCount { get; set; }
        public int RequiredCount { get; set; } = 1;
    }
}
