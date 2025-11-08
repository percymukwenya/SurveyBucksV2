namespace Domain.Models.Request
{
    public class CreateBankingDetailRequest
    {
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public bool IsPrimary { get; set; }
    }
}
