namespace Domain.Models.Response
{
    public class UserPointsDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public int RedeemedPoints { get; set; }
        public int ExpiredPoints { get; set; }
        public int PointsLevel { get; set; }
        public string LevelName { get; set; }
        public int PointsToNextLevel { get; set; }
        public decimal PointsMultiplier { get; set; }
    }
}
