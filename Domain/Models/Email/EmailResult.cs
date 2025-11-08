namespace Domain.Models.Email
{
    public class EmailResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string MessageId { get; set; }

        public static EmailResult Success(string messageId = null) => new EmailResult { IsSuccess = true, MessageId = messageId };
        public static EmailResult Failure(string errorMessage) => new EmailResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
