namespace Domain.Models
{
    public class UserDocumentSummaryDto
    {
        public string UserId { get; set; }
        public int TotalDocuments { get; set; }
        public int ApprovedDocuments { get; set; }
        public int PendingDocuments { get; set; }
        public int RejectedDocuments { get; set; }
        public int RequiredDocuments { get; set; }
        public int RequiredApproved { get; set; }
    }
}
