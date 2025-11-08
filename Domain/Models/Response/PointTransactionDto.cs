namespace Domain.Models.Response
{
    public class PointTransactionDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; }
        public string ActionType { get; set; }
        public string Description { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
