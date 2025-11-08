using Microsoft.AspNetCore.Http;

namespace Domain.Models
{
    public class DocumentUploadRequestDto
    {
        public int DocumentTypeId { get; set; }
        public IFormFile File { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
    }
}
