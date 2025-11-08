namespace Domain.Models
{
    public class FileUploadResult
    {
        public string FileName { get; set; }
        public string StoragePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
