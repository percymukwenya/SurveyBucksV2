namespace Domain.Models
{
    public class RewardRedemptionResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int? UserRewardId { get; set; }
        public string RedemptionCode { get; set; }
    }
}
