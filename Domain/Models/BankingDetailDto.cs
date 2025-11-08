namespace Domain.Models
{
    public class BankingDetailDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountNumberMasked { get; set; } // Only last 4 digits
        public string AccountType { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string SwiftCode { get; set; }
        public string RoutingNumber { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsVerified { get; set; }
        public string VerificationStatus { get; set; }
        public DateTimeOffset? VerifiedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBankingDetailDto
    {
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string SwiftCode { get; set; }
        public string RoutingNumber { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class UpdateBankingDetailDto
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountType { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string SwiftCode { get; set; }
        public string RoutingNumber { get; set; }
        public bool IsPrimary { get; set; }
    }
}
