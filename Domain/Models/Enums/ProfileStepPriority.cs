namespace Domain.Models.Enums
{
    public enum ProfileStepPriority
    {
        Critical = 1,  // Blocks survey access
        High = 2,      // Important for survey matching
        Medium = 3,    // Improves experience
        Low = 4        // Nice to have
    }
}
