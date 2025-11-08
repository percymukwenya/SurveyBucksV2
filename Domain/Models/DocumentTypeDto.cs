using Microsoft.AspNetCore.Http;

namespace Domain.Models
{
    public class DocumentTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsRequired { get; set; }
        public int MaxFileSizeMB { get; set; }
        public string AllowedFileTypes { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class UserDocumentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; }
        public string Category { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string VerificationStatus { get; set; }
        public string VerificationNotes { get; set; }
        public DateTimeOffset? VerifiedDate { get; set; }
        public string VerifiedBy { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public DateTimeOffset UploadedDate { get; set; }
        public bool IsRequired { get; set; }
        public string StoragePath { get; set; }
        public bool IsEncrypted { get; set; }
        
        // Admin properties
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class DocumentUploadDto
    {
        public int DocumentTypeId { get; set; }
        public IFormFile File { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class DocumentVerificationDto
    {
        public int DocumentId { get; set; }
        public string Status { get; set; } // Approved, Rejected
        public string Notes { get; set; }
    }
}
