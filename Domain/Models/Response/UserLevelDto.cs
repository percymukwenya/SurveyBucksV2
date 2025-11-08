namespace Domain.Models.Response
{
    public class UserLevelDto
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsRequired { get; set; }
        public string ImageUrl { get; set; }
        public decimal PointsMultiplier { get; set; }
        public string UnlocksRewardCategories { get; set; }
        public int CurrentUserPoints { get; set; }
        public int PointsToNextLevel { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
