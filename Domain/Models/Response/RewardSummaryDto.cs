namespace Domain.Models.Response
{
    public class RewardSummaryDto
    {
        public string RewardType { get; set; }
        public decimal? Amount { get; set; }
        public int? PointsAmount { get; set; }
        public string Description { get; set; }
    }
}