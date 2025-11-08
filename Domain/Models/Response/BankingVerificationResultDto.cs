namespace Domain.Models.Response
{
    public class BankingVerificationResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int BankingDetailId { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
        public DateTimeOffset VerifiedDate { get; set; }
    }
}