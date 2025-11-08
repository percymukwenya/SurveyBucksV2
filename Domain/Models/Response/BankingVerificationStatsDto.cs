namespace Domain.Models.Response
{
    public class BankingVerificationStatsDto
    {
        public int TotalBankingDetails { get; set; }
        public int PendingBankingDetails { get; set; }
        public int ApprovedBankingDetails { get; set; }
        public int RejectedBankingDetails { get; set; }
        public double AverageVerificationTimeHours { get; set; }
        public int BankingDetailsSubmittedToday { get; set; }
        public int BankingDetailsVerifiedToday { get; set; }
        public IEnumerable<BankStatsDto> TopBanks { get; set; } = new List<BankStatsDto>();
    }

    public class BankStatsDto
    {
        public string BankName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double ApprovalRate { get; set; }
    }
}