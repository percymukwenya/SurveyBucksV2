using Domain.Models;
using Domain.Models.Admin;

namespace Domain.Interfaces.Repository
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync();
        Task<DocumentTypeDto> GetDocumentTypeByIdAsync(int id);
        Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(string userId);
        Task<UserDocumentDto> GetUserDocumentByIdAsync(int id);
        Task<int> CreateUserDocumentAsync(UserDocumentDto document);
        Task<bool> UpdateDocumentVerificationAsync(int documentId, string status, string notes, string verifiedBy);
        Task<bool> DeleteUserDocumentAsync(int documentId, string deletedBy);
        Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId);
        Task<IEnumerable<DocumentVerificationHistoryDto>> GetDocumentVerificationHistoryAsync(int documentId);
        Task<bool> CreateVerificationHistoryAsync(DocumentVerificationHistoryDto history);

        Task<IEnumerable<UserDocumentDto>> GetDocumentsByTypeAndStatusAsync(string userId, int documentTypeId, string status);
        Task<string> GetDocumentCurrentStatusAsync(int documentId);
        Task<bool> UpdateDocumentStatusAsync(int documentId, string status, string notes, string verifiedBy);
        Task<UserDocumentSummaryDto> GetUserDocumentSummaryAsync(string userId);

        // Admin-specific methods
        Task<IEnumerable<UserDocumentDto>> GetPendingDocumentsAsync(int? documentTypeId = null, int pageSize = 50, int pageNumber = 1);
        Task<IEnumerable<UserDocumentDto>> GetDocumentsByStatusAsync(string status, int? documentTypeId = null, int pageSize = 50, int pageNumber = 1);
        Task<int> GetPendingDocumentsCountAsync(int? documentTypeId = null);
        Task<AdminDocumentStatsDto> GetDocumentVerificationStatsAsync();
        Task<IEnumerable<UserDocumentDto>> SearchDocumentsAsync(string searchTerm, string status = null, int pageSize = 50, int pageNumber = 1);
    }
}
