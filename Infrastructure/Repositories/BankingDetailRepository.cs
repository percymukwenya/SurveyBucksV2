using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Domain.Models.Response;
using Infrastructure.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class BankingDetailRepository : IBankingDetailRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public BankingDetailRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<BankingDetailDto>> GetUserBankingDetailsAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    Id, UserId, BankName, AccountHolderName, AccountNumber,
                    STUFF(AccountNumber, 1, LEN(AccountNumber) - 4, REPLICATE('*', LEN(AccountNumber) - 4)) AS AccountNumberMasked,
                    AccountType, BranchCode, BranchName, SwiftCode, RoutingNumber,
                    IsPrimary, IsVerified, VerificationStatus, VerifiedDate, IsActive
                FROM SurveyBucks.BankingDetail
                WHERE UserId = @UserId AND IsDeleted = 0
                ORDER BY IsPrimary DESC, CreatedDate DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<BankingDetailDto>(sql, new { UserId = userId });
        }

        public async Task<BankingDetailDto> GetBankingDetailByIdAsync(int id)
        {
            const string sql = @"
                SELECT 
                    Id, UserId, BankName, AccountHolderName, AccountNumber,
                    STUFF(AccountNumber, 1, LEN(AccountNumber) - 4, REPLICATE('*', LEN(AccountNumber) - 4)) AS AccountNumberMasked,
                    AccountType, BranchCode, BranchName, SwiftCode, RoutingNumber,
                    IsPrimary, IsVerified, VerificationStatus, VerifiedDate, IsActive
                FROM SurveyBucks.BankingDetail
                WHERE Id = @Id AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<BankingDetailDto>(sql, new { Id = id });
        }

        public async Task<int> CreateBankingDetailAsync(BankingDetailDto bankingDetail)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // If this is set as primary, update other records
                        if (bankingDetail.IsPrimary)
                        {
                            await connection.ExecuteAsync(
                                "UPDATE SurveyBucks.BankingDetail SET IsPrimary = 0 WHERE UserId = @UserId",
                                new { bankingDetail.UserId },
                                transaction);
                        }

                        const string insertSql = @"
                            INSERT INTO SurveyBucks.BankingDetail (
                                UserId, BankName, AccountHolderName, AccountNumber, AccountType,
                                BranchCode, BranchName, SwiftCode, RoutingNumber,
                                IsPrimary, IsVerified, VerificationStatus, IsActive,
                                CreatedDate, CreatedBy, IsDeleted
                            ) VALUES (
                                @UserId, @BankName, @AccountHolderName, @AccountNumber, @AccountType,
                                @BranchCode, @BranchName, @SwiftCode, @RoutingNumber,
                                @IsPrimary, 0, 'Pending', 1,
                                SYSDATETIMEOFFSET(), @UserId, 0
                            );
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        var id = await connection.ExecuteScalarAsync<int>(insertSql, bankingDetail, transaction);

                        // Log the creation
                        const string logSql = @"
                            INSERT INTO SurveyBucks.BankingDetailChangeLog (
                                BankingDetailId, FieldName, OldValue, NewValue,
                                ChangeType, ChangedBy, ChangedDate, CreatedDate
                            ) VALUES (
                                @Id, 'Created', NULL, 'New Banking Detail',
                                'Created', @UserId, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                            )";

                        await connection.ExecuteAsync(logSql, new { Id = id, bankingDetail.UserId }, transaction);

                        transaction.Commit();
                        return id;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateBankingDetailAsync(BankingDetailDto bankingDetail)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get existing record for comparison
                        var existing = await connection.QuerySingleOrDefaultAsync<BankingDetailDto>(
                            "SELECT * FROM SurveyBucks.BankingDetail WHERE Id = @Id",
                            new { bankingDetail.Id },
                            transaction);

                        if (existing == null) return false;

                        // Update record
                        const string updateSql = @"
                            UPDATE SurveyBucks.BankingDetail
                            SET BankName = @BankName,
                                AccountHolderName = @AccountHolderName,
                                AccountType = @AccountType,
                                BranchCode = @BranchCode,
                                BranchName = @BranchName,
                                SwiftCode = @SwiftCode,
                                RoutingNumber = @RoutingNumber,
                                ModifiedDate = SYSDATETIMEOFFSET(),
                                ModifiedBy = @UserId
                            WHERE Id = @Id";

                        await connection.ExecuteAsync(updateSql, bankingDetail, transaction);

                        // Log changes
                        var changes = new List<(string Field, string OldValue, string NewValue)>();

                        if (existing.BankName != bankingDetail.BankName)
                            changes.Add(("BankName", existing.BankName, bankingDetail.BankName));
                        if (existing.AccountHolderName != bankingDetail.AccountHolderName)
                            changes.Add(("AccountHolderName", existing.AccountHolderName, bankingDetail.AccountHolderName));
                        // Add other field comparisons...

                        foreach (var change in changes)
                        {
                            await connection.ExecuteAsync(@"
                                INSERT INTO SurveyBucks.BankingDetailChangeLog (
                                    BankingDetailId, FieldName, OldValue, NewValue,
                                    ChangeType, ChangedBy, ChangedDate, CreatedDate
                                ) VALUES (
                                    @Id, @Field, @OldValue, @NewValue,
                                    'Updated', @UserId, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                                )",
                                new
                                {
                                    bankingDetail.Id,
                                    change.Field,
                                    change.OldValue,
                                    change.NewValue,
                                    bankingDetail.UserId
                                },
                                transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> DeleteBankingDetailAsync(int id, string deletedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.BankingDetail
                SET IsDeleted = 1,
                    IsActive = 0,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @Id";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy });
                return result > 0;
            }
        }

        public async Task<bool> SetPrimaryBankingDetailAsync(int id, string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Remove primary from all other accounts
                        await connection.ExecuteAsync(
                            "UPDATE SurveyBucks.BankingDetail SET IsPrimary = 0 WHERE UserId = @UserId AND Id != @Id",
                            new { UserId = userId, Id = id },
                            transaction);

                        // Set this one as primary
                        await connection.ExecuteAsync(
                            "UPDATE SurveyBucks.BankingDetail SET IsPrimary = 1 WHERE Id = @Id AND UserId = @UserId",
                            new { Id = id, UserId = userId },
                            transaction);

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> VerifyBankingDetailAsync(int id, string verifiedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.BankingDetail
                SET IsVerified = 1,
                    VerificationStatus = 'Verified',
                    VerifiedDate = SYSDATETIMEOFFSET(),
                    VerifiedBy = @VerifiedBy,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @VerifiedBy
                WHERE Id = @Id";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { Id = id, VerifiedBy = verifiedBy });
                return result > 0;
            }
        }

        public async Task<BankingDetailDto> GetPrimaryBankingDetailAsync(string userId)
        {
            const string sql = @"
                SELECT TOP 1
                    Id, UserId, BankName, AccountHolderName, AccountNumber,
                    STUFF(AccountNumber, 1, LEN(AccountNumber) - 4, REPLICATE('*', LEN(AccountNumber) - 4)) AS AccountNumberMasked,
                    AccountType, BranchCode, BranchName, SwiftCode, RoutingNumber,
                    IsPrimary, IsVerified, VerificationStatus, VerifiedDate, IsActive
                FROM SurveyBucks.BankingDetail
                WHERE UserId = @UserId AND IsPrimary = 1 AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<BankingDetailDto>(sql, new { UserId = userId });
            }
        }

        public async Task<bool> UpdateBankingVerificationAsync(int id, string status, string notes, string verifiedBy, bool isVerified)
        {
            const string sql = @"
                UPDATE SurveyBucks.BankingDetail
                SET VerificationStatus = @Status,
                    VerificationNotes = @Notes,
                    VerifiedDate = CASE WHEN @Status IN ('Approved', 'Rejected') THEN SYSDATETIMEOFFSET() ELSE VerifiedDate END,
                    VerifiedBy = @VerifiedBy,
                    IsVerified = @IsVerified,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @VerifiedBy
                WHERE Id = @Id";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = await connection.ExecuteAsync(sql, new { 
                    Id = id, 
                    Status = status, 
                    Notes = notes, 
                    VerifiedBy = verifiedBy,
                    IsVerified = isVerified 
                });
                return result > 0;
            }
        }

        public async Task<BankingVerificationStatsDto> GetBankingVerificationStatsAsync()
        {
            const string sql = @"
                DECLARE @Today DATE = CAST(SYSDATETIMEOFFSET() AS DATE);
                
                SELECT 
                    COUNT(*) as TotalBankingDetails,
                    SUM(CASE WHEN VerificationStatus = 'Pending' THEN 1 ELSE 0 END) as PendingBankingDetails,
                    SUM(CASE WHEN VerificationStatus = 'Approved' THEN 1 ELSE 0 END) as ApprovedBankingDetails,
                    SUM(CASE WHEN VerificationStatus = 'Rejected' THEN 1 ELSE 0 END) as RejectedBankingDetails,
                    AVG(CASE 
                        WHEN VerifiedDate IS NOT NULL AND CreatedDate IS NOT NULL 
                        THEN DATEDIFF(HOUR, CreatedDate, VerifiedDate) 
                        ELSE NULL 
                    END) as AverageVerificationTimeHours,
                    SUM(CASE WHEN CAST(CreatedDate AS DATE) = @Today THEN 1 ELSE 0 END) as BankingDetailsSubmittedToday,
                    SUM(CASE WHEN CAST(VerifiedDate AS DATE) = @Today AND VerificationStatus IN ('Approved', 'Rejected') THEN 1 ELSE 0 END) as BankingDetailsVerifiedToday
                FROM SurveyBucks.BankingDetail 
                WHERE IsDeleted = 0;

                -- Top banks stats
                SELECT 
                    BankName,
                    COUNT(*) as Count,
                    CAST(SUM(CASE WHEN VerificationStatus = 'Approved' THEN 1.0 ELSE 0.0 END) / COUNT(*) * 100 AS DECIMAL(5,2)) as ApprovalRate
                FROM SurveyBucks.BankingDetail 
                WHERE IsDeleted = 0 AND VerificationStatus IN ('Approved', 'Rejected')
                GROUP BY BankName
                HAVING COUNT(*) >= 2
                ORDER BY Count DESC, ApprovalRate DESC;";

            using (var connection = _connectionFactory.CreateConnection())
            {
                var multi = await connection.QueryMultipleAsync(sql);
                
                var stats = await multi.ReadSingleAsync<BankingVerificationStatsDto>();
                var topBanks = await multi.ReadAsync<BankStatsDto>();
                
                stats.TopBanks = topBanks;
                return stats;
            }
        }

        public async Task<IEnumerable<BankingDetailDto>> GetBankingDetailsByStatusAsync(string status, int pageSize = 50, int pageNumber = 1)
        {
            const string sql = @"
                select
                bd.Id, bd.UserId, bd.BankName, bd.AccountHolderName, bd.AccountNumber,
                    bd.AccountType, bd.BranchCode, bd.BranchName, bd.SwiftCode, bd.RoutingNumber,
                    bd.IsPrimary, bd.IsVerified, bd.VerificationStatus,
                    bd.VerifiedDate, bd.VerifiedBy, bd.CreatedDate, bd.ModifiedDate,
                    u.Email as UserEmail, u.FirstName + ' ' + u.LastName as UserName
                FROM SurveyBucks.BankingDetail bd
                INNER JOIN SurveyBucks.Users u ON bd.UserId = u.Id
                WHERE bd.VerificationStatus = @Status AND bd.IsDeleted = 0
                ORDER BY bd.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var offset = (pageNumber - 1) * pageSize;

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<BankingDetailDto>(sql, new { 
                    Status = status, 
                    Offset = offset, 
                    PageSize = pageSize 
                });
            }
        }

        public async Task<IEnumerable<BankingDetailDto>> SearchBankingDetailsAsync(string searchTerm, string status = "", int pageSize = 50, int pageNumber = 1)
        {
            var whereClause = "WHERE bd.IsDeleted = 0";
            var parameters = new Dictionary<string, object> 
            { 
                { "SearchTerm", $"%{searchTerm}%" },
                { "Offset", (pageNumber - 1) * pageSize },
                { "PageSize", pageSize }
            };

            if (!string.IsNullOrEmpty(status))
            {
                whereClause += " AND bd.VerificationStatus = @Status";
                parameters.Add("Status", status);
            }

            var sql = $@"
                SELECT 
                    bd.Id, bd.UserId, bd.BankName, bd.AccountHolderName, bd.AccountNumber,
                    bd.AccountType, bd.BranchCode, bd.BranchName, bd.SwiftCode, bd.RoutingNumber,
                    bd.IsPrimary, bd.IsVerified, bd.VerificationStatus, bd.VerificationNotes,
                    bd.VerifiedDate, bd.VerifiedBy, bd.CreatedDate, bd.ModifiedDate,
                    u.Email as UserEmail, u.FirstName + ' ' + u.LastName as UserName
                FROM SurveyBucks.BankingDetail bd
                INNER JOIN SurveyBucks.User u ON bd.UserId = u.Id
                {whereClause}
                AND (bd.BankName LIKE @SearchTerm 
                     OR bd.AccountHolderName LIKE @SearchTerm 
                     OR u.Email LIKE @SearchTerm
                     OR u.FirstName LIKE @SearchTerm
                     OR u.LastName LIKE @SearchTerm)
                ORDER BY bd.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<BankingDetailDto>(sql, parameters);
            }
        }
    }
}
