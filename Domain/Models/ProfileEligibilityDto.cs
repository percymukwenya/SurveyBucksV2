namespace Domain.Models
{
    public class ProfileEligibilityDto
    {
        public bool IsEligible { get; set; }
        public int CompletionPercentage { get; set; }
        public int RequiredPercentage { get; set; }
        public List<ProfileNextStepDto> NextSteps { get; set; } = new();
        public string Message { get; set; }
    }
}
