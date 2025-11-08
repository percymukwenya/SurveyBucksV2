namespace Domain.Models.Response
{
    public class RewardDto
    {
        public int Id { get; set; }
        public int? SurveyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public string RewardType { get; set; }
        public string RewardCategory { get; set; }
        public int? PointsCost { get; set; }
        public int? AvailableQuantity { get; set; }
        public decimal? MonetaryValue { get; set; }
        public string ImageUrl { get; set; }
        public string RedemptionInstructions { get; set; }
        public int? MinimumUserLevel { get; set; }
    }
}
