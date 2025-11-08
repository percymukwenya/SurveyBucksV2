namespace Domain.Models
{
    public class FileUploadResultDto
    {
        public bool Success { get; set; }
        public string FileName { get; set; }
        public string StoragePath { get; set; }
        public string BlobName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
