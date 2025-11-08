namespace Domain.Models.Admin
{
    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string ActivityType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionUrl { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string InitiatedBy { get; set; }
    }
}
