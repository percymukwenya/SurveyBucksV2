namespace Domain.Models.Response
{
    public class UserAchievementDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public int PointsAwarded { get; set; }
        public string RequiredActionType { get; set; }
        public int RequiredActionCount { get; set; }
        public bool IsRepeatable { get; set; }
        public int? RepeatCooldownDays { get; set; }
    }
}
