namespace Domain.Models.Response
{
    public class UserRewardDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int RewardId { get; set; }
        public string RewardName { get; set; }
        public string RewardDescription { get; set; }
        public string RewardType { get; set; }
        public string RedemptionMethod { get; set; }
        public decimal? RewardAmount { get; set; }
        public DateTime EarnedDate { get; set; }
        public string RedemptionStatus { get; set; }
        public DateTime? ClaimedDate { get; set; }
        public string RedemptionCode { get; set; }
        public string DeliveryStatus { get; set; }
        public int SurveyId { get; set; }
        public int? PointsUsed { get; set; }
        public decimal? MonetaryValueRedeemed { get; set; }

    }
}
