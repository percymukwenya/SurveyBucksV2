namespace Domain.Models.Response
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int NotificationTypeId { get; set; }
        public string NotificationTypeName { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public string DeepLink { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? ReadDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
