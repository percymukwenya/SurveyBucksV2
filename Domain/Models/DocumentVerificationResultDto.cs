namespace Domain.Models
{
    public class DocumentVerificationResultDto
    {
        public int DocumentId { get; set; }
        public bool Success { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
