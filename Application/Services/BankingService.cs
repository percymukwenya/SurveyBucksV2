using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Response;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Domain.Models.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services
{
    public class BankingService : IBankingService
    {
        private readonly IBankingDetailRepository _bankingRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<BankingService> _logger;
        private readonly IMemoryCache _cache;

        public BankingService(
            IBankingDetailRepository bankingRepository,
            INotificationRepository notificationRepository,
            ILogger<BankingService> logger,
            IMemoryCache cache)
        {
            _bankingRepository = bankingRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<BankingDetailDto>> GetUserBankingDetailsAsync(string userId)
        {
            return await _bankingRepository.GetUserBankingDetailsAsync(userId);
        }

        public async Task<BankingDetailDto> GetBankingDetailByIdAsync(int id, string userId)
        {
            var bankingDetail = await _bankingRepository.GetBankingDetailByIdAsync(id);

            if (bankingDetail == null)
            {
                throw new NotFoundException($"Banking detail with ID {id} not found");
            }

            if (bankingDetail.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only access your own banking details");
            }

            return bankingDetail;
        }

        public async Task<BankingDetailDto> CreateBankingDetailAsync(string userId, CreateBankingDetailDto createDto)
        {
            // Validate banking details
            ValidateBankingDetail(createDto);

            // Check if user already has maximum allowed banking details (e.g., 3)
            var existingDetails = await _bankingRepository.GetUserBankingDetailsAsync(userId);
            if (existingDetails.Count() >= 3)
            {
                throw new InvalidOperationException("Maximum number of banking details (3) already reached");
            }

            var bankingDetail = new BankingDetailDto
            {
                UserId = userId,
                BankName = createDto.BankName,
                AccountHolderName = createDto.AccountHolderName,
                AccountNumber = createDto.AccountNumber,
                AccountType = createDto.AccountType,
                BranchCode = createDto.BranchCode,
                BranchName = createDto.BranchName,
                SwiftCode = createDto.SwiftCode,
                RoutingNumber = createDto.RoutingNumber,
                IsPrimary = createDto.IsPrimary || !existingDetails.Any() // First account is always primary
            };

            var id = await _bankingRepository.CreateBankingDetailAsync(bankingDetail);

            // Send notification
            await _notificationRepository.CreateNotificationAsync(
                userId,
                "Banking Details Added",
                "Your banking details have been added successfully and are pending verification.",
                "BankingDetails",
                id.ToString(),
                "Banking");

            return await _bankingRepository.GetBankingDetailByIdAsync(id);
        }

        public async Task<bool> UpdateBankingDetailAsync(string userId, UpdateBankingDetailDto updateDto)
        {
            // Verify ownership
            var existing = await GetBankingDetailByIdAsync(updateDto.Id, userId);

            if (existing.IsVerified)
            {
                throw new InvalidOperationException("Cannot update verified banking details. Please add new banking details instead.");
            }

            // Validate banking details
            ValidateBankingDetail(updateDto);

            var bankingDetail = new BankingDetailDto
            {
                Id = updateDto.Id,
                UserId = userId,
                BankName = updateDto.BankName,
                AccountHolderName = updateDto.AccountHolderName,
                AccountType = updateDto.AccountType,
                BranchCode = updateDto.BranchCode,
                BranchName = updateDto.BranchName,
                SwiftCode = updateDto.SwiftCode,
                RoutingNumber = updateDto.RoutingNumber,
                IsPrimary = updateDto.IsPrimary
            };

            return await _bankingRepository.UpdateBankingDetailAsync(bankingDetail);
        }

        public async Task<bool> DeleteBankingDetailAsync(int id, string userId)
        {
            var bankingDetail = await GetBankingDetailByIdAsync(id, userId);

            if (bankingDetail.IsPrimary)
            {
                // Check if there are other banking details
                var allDetails = await _bankingRepository.GetUserBankingDetailsAsync(userId);
                if (allDetails.Count() > 1)
                {
                    throw new InvalidOperationException("Cannot delete primary banking detail. Please set another account as primary first.");
                }
            }

            return await _bankingRepository.DeleteBankingDetailAsync(id, userId);
        }

        public async Task<bool> SetPrimaryBankingDetailAsync(int id, string userId)
        {
            // Verify ownership
            await GetBankingDetailByIdAsync(id, userId);

            return await _bankingRepository.SetPrimaryBankingDetailAsync(id, userId);
        }

        public async Task<bool> VerifyBankingDetailAsync(int id, string verifiedBy)
        {
            var result = await _bankingRepository.VerifyBankingDetailAsync(id, verifiedBy);

            if (result)
            {
                // Get banking detail to send notification
                var bankingDetail = await _bankingRepository.GetBankingDetailByIdAsync(id);

                await _notificationRepository.CreateNotificationAsync(
                    bankingDetail.UserId,
                    "Banking Details Verified",
                    "Your banking details have been verified successfully.",
                    "BankingDetails",
                    id.ToString(),
                    "Banking");
            }

            return result;
        }

        public async Task<BankingVerificationResultDto> VerifyBankingDetailAsync(int id, string status, string notes, string verifiedBy)
        {
            _logger.LogInformation("Verifying banking detail {BankingDetailId} with status {Status} by {VerifiedBy}", id, status, verifiedBy);

            try
            {
                // Get banking detail first
                var bankingDetail = await _bankingRepository.GetBankingDetailByIdAsync(id);
                if (bankingDetail == null)
                {
                    return new BankingVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Banking detail not found"
                    };
                }

                // Check if already verified
                if (bankingDetail.IsVerified && bankingDetail.VerificationStatus == VerificationStatus.Approved)
                {
                    return new BankingVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Banking detail is already verified"
                    };
                }

                var previousStatus = bankingDetail.VerificationStatus;
                var isApproved = status.Equals(VerificationStatus.Approved, StringComparison.OrdinalIgnoreCase);

                // Update banking detail
                var updateResult = await _bankingRepository.UpdateBankingVerificationAsync(id, status, notes, verifiedBy, isApproved);
                if (!updateResult)
                {
                    return new BankingVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Failed to update banking detail status"
                    };
                }

                // Create notification
                var notificationMessage = isApproved 
                    ? "Your banking details have been approved and are ready for payments."
                    : $"Your banking details were rejected. Reason: {notes}";

                await _notificationRepository.CreateNotificationAsync(
                    bankingDetail.UserId,
                    isApproved ? "Banking Details Approved" : "Banking Details Rejected",
                    notificationMessage,
                    "BankingDetails",
                    id.ToString(),
                    "Banking");

                // Invalidate profile completion cache when banking is verified
                _cache.Remove($"profile_completion_{bankingDetail.UserId}");
                _logger.LogInformation("Invalidated profile completion cache for user {UserId} after banking verification", bankingDetail.UserId);

                _logger.LogInformation("Banking detail {BankingDetailId} {Status} successfully", id, status);

                return new BankingVerificationResultDto
                {
                    Success = true,
                    BankingDetailId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = status,
                    VerifiedBy = verifiedBy,
                    VerifiedDate = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying banking detail {BankingDetailId}", id);
                return new BankingVerificationResultDto
                {
                    Success = false,
                    ErrorMessage = "Internal server error during verification"
                };
            }
        }

        public async Task<BankingVerificationStatsDto> GetBankingVerificationStatsAsync()
        {
            try
            {
                var stats = await _bankingRepository.GetBankingVerificationStatsAsync();
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banking verification stats");
                throw;
            }
        }

        public async Task<IEnumerable<BankingDetailDto>> GetBankingDetailsByStatusAsync(string status, int pageSize = 50, int pageNumber = 1)
        {
            try
            {
                return await _bankingRepository.GetBankingDetailsByStatusAsync(status, pageSize, pageNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banking details by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<BankingDetailDto>> SearchBankingDetailsAsync(string searchTerm, string status = "", int pageSize = 50, int pageNumber = 1)
        {
            try
            {
                return await _bankingRepository.SearchBankingDetailsAsync(searchTerm, status, pageSize, pageNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching banking details with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        private void ValidateBankingDetail(CreateBankingDetailDto bankingDetail)
        {
            if (string.IsNullOrWhiteSpace(bankingDetail.BankName))
                throw new ArgumentException("Bank name is required");

            if (string.IsNullOrWhiteSpace(bankingDetail.AccountHolderName))
                throw new ArgumentException("Account holder name is required");

            if (string.IsNullOrWhiteSpace(bankingDetail.AccountNumber))
                throw new ArgumentException("Account number is required");

            if (string.IsNullOrWhiteSpace(bankingDetail.AccountType))
                throw new ArgumentException("Account type is required");

            // Validate account type
            var validAccountTypes = new[] { AccountType.Savings, AccountType.Current, AccountType.Cheque };
            if (!validAccountTypes.Contains(bankingDetail.AccountType))
                throw new ArgumentException("Invalid account type");

            // Basic account number validation (you might want to add country-specific validation)
            if (bankingDetail.AccountNumber.Length < 6 || bankingDetail.AccountNumber.Length > 20)
                throw new ArgumentException("Account number must be between 6 and 20 characters");

            // Add more validation as needed based on your requirements
        }

        private void ValidateBankingDetail(UpdateBankingDetailDto bankingDetail)
        {
            ValidateBankingDetail(new CreateBankingDetailDto
            {
                BankName = bankingDetail.BankName,
                AccountHolderName = bankingDetail.AccountHolderName,
                AccountNumber = "123456", // Dummy value since we don't update account number
                AccountType = bankingDetail.AccountType,
                BranchCode = bankingDetail.BranchCode,
                BranchName = bankingDetail.BranchName,
                SwiftCode = bankingDetail.SwiftCode,
                RoutingNumber = bankingDetail.RoutingNumber
            });
        }
    }
}
