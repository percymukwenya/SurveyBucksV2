namespace Domain.Models.Admin
{
    public class ActivityLogCreateDto
    {
        public string ActivityType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ActionUrl { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string UserId { get; set; }
        public string CreatedBy { get; set; }
    }
}
