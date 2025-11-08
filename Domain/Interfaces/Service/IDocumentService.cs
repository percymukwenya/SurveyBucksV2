using Domain.Models;
using Domain.Models.Admin;

namespace Domain.Interfaces.Service
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync();
        Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(string userId);
        Task<UserDocumentDto> GetUserDocumentByIdAsync(int id, string userId);
        Task<DocumentUploadResultDto> UploadDocumentAsync(string userId, DocumentUploadRequestDto uploadRequest);
        Task<bool> DeleteDocumentAsync(int documentId, string userId);
        Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId);
        Task<Stream> DownloadDocumentAsync(int documentId, string userId);

        // Admin functions
        Task<DocumentVerificationResultDto> VerifyDocumentAsync(int documentId, string status, string notes, string verifiedBy);
        Task<IEnumerable<DocumentVerificationHistoryDto>> GetDocumentHistoryAsync(int documentId);
        
        // Admin document management
        Task<IEnumerable<UserDocumentDto>> GetPendingDocumentsAsync(int? documentTypeId = null, int pageSize = 50, int pageNumber = 1);
        Task<IEnumerable<UserDocumentDto>> GetDocumentsByStatusAsync(string status, int? documentTypeId = null, int pageSize = 50, int pageNumber = 1);
        Task<int> GetPendingDocumentsCountAsync(int? documentTypeId = null);
        Task<AdminDocumentStatsDto> GetDocumentVerificationStatsAsync();
        Task<IEnumerable<UserDocumentDto>> SearchDocumentsAsync(string searchTerm, string status = null, int pageSize = 50, int pageNumber = 1);
        Task<(Stream FileStream, string FileName, string ContentType)> DownloadDocumentForAdminAsync(int documentId);
    }
}
