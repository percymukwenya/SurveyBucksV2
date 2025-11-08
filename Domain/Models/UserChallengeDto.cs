namespace Domain.Models
{
    public class UserChallengeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string RequiredActionType { get; set; }
        public int RequiredActionCount { get; set; }
        public int PointsAwarded { get; set; }
        public int RewardId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
