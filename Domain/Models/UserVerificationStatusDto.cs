namespace Domain.Models
{
    public class UserVerificationStatusDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public bool HasVerifiedIdentity { get; set; }
        public bool HasVerifiedBanking { get; set; }
        public string MissingRequiredDocuments { get; set; }
        public bool IsFullyVerified { get; set; }
        public List<UserDocumentDto> Documents { get; set; }
        public List<BankingDetailDto> BankingDetails { get; set; }
    }

    public class DocumentVerificationHistoryDto
    {
        public int Id { get; set; }
        public int UserDocumentId { get; set; }
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public string Notes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTimeOffset VerifiedDate { get; set; }
    }
}
