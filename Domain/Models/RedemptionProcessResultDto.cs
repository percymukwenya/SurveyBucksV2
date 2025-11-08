namespace Domain.Models
{
    public class RedemptionProcessResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int UserRewardId { get; set; }
        public string RedemptionCode { get; set; }
    }
}
