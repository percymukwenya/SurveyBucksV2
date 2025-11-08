using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Repository
{
    public interface IBankingDetailRepository
    {
        Task<IEnumerable<BankingDetailDto>> GetUserBankingDetailsAsync(string userId);
        Task<BankingDetailDto> GetBankingDetailByIdAsync(int id);
        Task<int> CreateBankingDetailAsync(BankingDetailDto bankingDetail);
        Task<bool> UpdateBankingDetailAsync(BankingDetailDto bankingDetail);
        Task<bool> DeleteBankingDetailAsync(int id, string deletedBy);
        Task<bool> SetPrimaryBankingDetailAsync(int id, string userId);
        Task<bool> VerifyBankingDetailAsync(int id, string verifiedBy);
        Task<BankingDetailDto> GetPrimaryBankingDetailAsync(string userId);
        
        // Admin functions for verification
        Task<bool> UpdateBankingVerificationAsync(int id, string status, string notes, string verifiedBy, bool isVerified);
        Task<BankingVerificationStatsDto> GetBankingVerificationStatsAsync();
        Task<IEnumerable<BankingDetailDto>> GetBankingDetailsByStatusAsync(string status, int pageSize = 50, int pageNumber = 1);
        Task<IEnumerable<BankingDetailDto>> SearchBankingDetailsAsync(string searchTerm, string status = "", int pageSize = 50, int pageNumber = 1);
    }
}
