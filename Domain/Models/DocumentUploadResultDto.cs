namespace Domain.Models
{
    public class DocumentUploadResultDto
    {
        public bool Success { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
    }
}
