namespace Domain.Models.Response
{
    public class ChallengeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public int ChallengeId { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletedDate { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsRewarded { get; set; }
        public string RequiredActionType { get; set; }
        public int RequiredActionCount { get; set; }
    }
}
