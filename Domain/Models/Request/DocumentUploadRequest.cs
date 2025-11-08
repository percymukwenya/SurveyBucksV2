using Microsoft.AspNetCore.Http;

namespace Domain.Models.Request
{
    public class DocumentUploadRequest
    {
        public int DocumentTypeId { get; set; }
        public IFormFile File { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
