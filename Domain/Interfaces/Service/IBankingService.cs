using Domain.Models;
using Domain.Models.Response;

namespace Domain.Interfaces.Service
{
    public interface IBankingService
    {
        Task<IEnumerable<BankingDetailDto>> GetUserBankingDetailsAsync(string userId);
        Task<BankingDetailDto> GetBankingDetailByIdAsync(int id, string userId);
        Task<BankingDetailDto> CreateBankingDetailAsync(string userId, CreateBankingDetailDto bankingDetail);
        Task<bool> UpdateBankingDetailAsync(string userId, UpdateBankingDetailDto bankingDetail);
        Task<bool> DeleteBankingDetailAsync(int id, string userId);
        Task<bool> SetPrimaryBankingDetailAsync(int id, string userId);

        // Admin functions
        Task<bool> VerifyBankingDetailAsync(int id, string verifiedBy);
        Task<BankingVerificationResultDto> VerifyBankingDetailAsync(int id, string status, string notes, string verifiedBy);
        Task<BankingVerificationStatsDto> GetBankingVerificationStatsAsync();
        Task<IEnumerable<BankingDetailDto>> GetBankingDetailsByStatusAsync(string status, int pageSize = 50, int pageNumber = 1);
        Task<IEnumerable<BankingDetailDto>> SearchBankingDetailsAsync(string searchTerm, string status = "", int pageSize = 50, int pageNumber = 1);
    }
}
