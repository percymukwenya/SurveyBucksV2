using Dapper;
using Domain.Interfaces.Repository;
using Domain.Models;
using Domain.Models.Admin;
using Infrastructure.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    // Helper class to map stored procedure results
    internal class VerificationStatusResult
    {
        public string Email { get; set; }
        public int HasVerifiedIdentity { get; set; }
        public int HasVerifiedBanking { get; set; }
        public string MissingRequiredDocuments { get; set; }
        public int IsFullyVerified { get; set; }
    }

    public class DocumentRepository : IDocumentRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public DocumentRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync()
        {
            const string sql = @"
                SELECT Id, Name, Description, Category, IsRequired, 
                       MaxFileSizeMB, AllowedFileTypes, IsActive, DisplayOrder
                FROM SurveyBucks.DocumentType
                WHERE IsActive = 1 AND IsDeleted = 0
                ORDER BY DisplayOrder";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<DocumentTypeDto>(sql);
        }

        public async Task<DocumentTypeDto> GetDocumentTypeByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Name, Description, Category, IsRequired, 
                       MaxFileSizeMB, AllowedFileTypes, IsActive, DisplayOrder
                FROM SurveyBucks.DocumentType
                WHERE Id = @Id AND IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<DocumentTypeDto>(sql, new { Id = id });
            }
        }

        public async Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(string userId)
        {
            const string sql = @"
                SELECT 
                    ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                    dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                    ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                    ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                    dt.IsRequired
                FROM SurveyBucks.UserDocument ud
                JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                WHERE ud.UserId = @UserId AND ud.IsDeleted = 0
                ORDER BY dt.DisplayOrder";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserDocumentDto>(sql, new { UserId = userId });
        }

        public async Task<UserDocumentDto> GetUserDocumentByIdAsync(int id)
        {
            const string sql = @"
                SELECT 
                    ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                    dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                    ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                    ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                    dt.IsRequired, ud.StoragePath
                FROM SurveyBucks.UserDocument ud
                JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                WHERE ud.Id = @Id AND ud.IsDeleted = 0";

            using (var connection = _connectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<UserDocumentDto>(sql, new { Id = id });
            }
        }

        public async Task<int> CreateUserDocumentAsync(UserDocumentDto document)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.UserDocument (
                UserId, DocumentTypeId, FileName, OriginalFileName, FileSize,
                ContentType, StoragePath, VerificationStatus, IsEncrypted, 
                UploadedDate, ExpiryDate
            ) VALUES (
                @UserId, @DocumentTypeId, @FileName, @OriginalFileName, @FileSize,
                @ContentType, @StoragePath, @VerificationStatus, @IsEncrypted,
                @UploadedDate, @ExpiryDate
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, document);
        }

        public async Task<bool> UpdateDocumentStatusAsync(int documentId, string status, string notes, string verifiedBy)
        {
            const string sql = @"
            UPDATE SurveyBucks.UserDocument
            SET VerificationStatus = @Status,
                VerificationNotes = @Notes,
                VerifiedDate = CASE WHEN @Status IN ('Approved', 'Rejected') THEN SYSDATETIMEOFFSET() ELSE VerifiedDate END,
                VerifiedBy = @VerifiedBy,
                ModifiedDate = SYSDATETIMEOFFSET(),
                ModifiedBy = @VerifiedBy
            WHERE Id = @Id";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                Id = documentId,
                Status = status,
                Notes = notes,
                VerifiedBy = verifiedBy
            });

            return result > 0;
        }

        public async Task<bool> CreateVerificationHistoryAsync(DocumentVerificationHistoryDto history)
        {
            const string sql = @"
            INSERT INTO SurveyBucks.DocumentVerificationHistory (
                UserDocumentId, PreviousStatus, NewStatus, Notes,
                VerifiedBy, VerifiedDate, CreatedDate
            ) VALUES (
                @UserDocumentId, @PreviousStatus, @NewStatus, @Notes,
                @VerifiedBy, @VerifiedDate, GETDATE()
            )";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, history);
            return result > 0;
        }

        public async Task<bool> UpdateDocumentVerificationAsync(int documentId, string status, string notes, string verifiedBy)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get current status
                        var currentStatus = await connection.ExecuteScalarAsync<string>(
                            "SELECT VerificationStatus FROM SurveyBucks.UserDocument WHERE Id = @Id",
                            new { Id = documentId },
                            transaction);

                        // Update document status
                        const string updateSql = @"
                            UPDATE SurveyBucks.UserDocument
                            SET VerificationStatus = @Status,
                                VerificationNotes = @Notes,
                                VerifiedDate = SYSDATETIMEOFFSET(),
                                VerifiedBy = @VerifiedBy,
                                ModifiedDate = SYSDATETIMEOFFSET(),
                                ModifiedBy = @VerifiedBy
                            WHERE Id = @Id";

                        await connection.ExecuteAsync(updateSql,
                            new { Id = documentId, Status = status, Notes = notes, VerifiedBy = verifiedBy },
                            transaction);

                        // Add to verification history
                        const string historySql = @"
                            INSERT INTO SurveyBucks.DocumentVerificationHistory (
                                UserDocumentId, PreviousStatus, NewStatus, Notes,
                                VerifiedBy, VerifiedDate, CreatedDate
                            ) VALUES (
                                @DocumentId, @PreviousStatus, @NewStatus, @Notes,
                                @VerifiedBy, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                            )";

                        await connection.ExecuteAsync(historySql,
                            new
                            {
                                DocumentId = documentId,
                                PreviousStatus = currentStatus,
                                NewStatus = status,
                                Notes = notes,
                                VerifiedBy = verifiedBy
                            },
                            transaction);

                        // Send notification
                        const string getUserSql = "SELECT UserId FROM SurveyBucks.UserDocument WHERE Id = @Id";
                        var userId = await connection.ExecuteScalarAsync<string>(getUserSql, new { Id = documentId }, transaction);

                        const string notificationSql = @"
                            INSERT INTO SurveyBucks.UserNotification (
                                UserId, NotificationTypeId, Title, Message,
                                ReferenceId, ReferenceType, CreatedDate, CreatedBy
                            )
                            SELECT 
                                @UserId, nt.Id, 
                                CASE WHEN @Status = 'Approved' THEN 'Document Approved' ELSE 'Document Rejected' END,
                                CASE WHEN @Status = 'Approved' 
                                    THEN 'Your document has been approved successfully.'
                                    ELSE 'Your document was rejected. ' + ISNULL(@Notes, 'Please upload a new document.')
                                END,
                                @DocumentId, 'Document',
                                SYSDATETIMEOFFSET(), 'system'
                            FROM SurveyBucks.NotificationType nt
                            WHERE nt.Name = 'DocumentVerification'";

                        await connection.ExecuteAsync(notificationSql,
                            new { UserId = userId, Status = status, Notes = notes, DocumentId = documentId.ToString() },
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

        public async Task<bool> DeleteUserDocumentAsync(int documentId, string deletedBy)
        {
            const string sql = @"
                UPDATE SurveyBucks.UserDocument
                SET IsDeleted = 1,
                    ModifiedDate = SYSDATETIMEOFFSET(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @Id";

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new { Id = documentId, DeletedBy = deletedBy });
            return result > 0;
        }

        public async Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                var result = new UserVerificationStatusDto { UserId = userId };

                // Execute stored procedure
                using (var multi = await connection.QueryMultipleAsync(
                    "SurveyBucks.up_GetUserVerificationStatus",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure))
                {
                    // First result set - overall status with explicit type mapping
                    var status = await multi.ReadSingleOrDefaultAsync<VerificationStatusResult>();
                    if (status != null)
                    {
                        result.Email = status.Email;
                        result.HasVerifiedIdentity = status.HasVerifiedIdentity == 1;
                        result.HasVerifiedBanking = status.HasVerifiedBanking == 1;
                        result.MissingRequiredDocuments = status.MissingRequiredDocuments ?? "";
                        result.IsFullyVerified = status.IsFullyVerified == 1;
                    }

                    // Second result set - documents
                    result.Documents = (await multi.ReadAsync<UserDocumentDto>()).ToList();

                    // Third result set - banking details
                    result.BankingDetails = (await multi.ReadAsync<BankingDetailDto>()).ToList();
                }

                return result;
            }
        }

        public async Task<IEnumerable<DocumentVerificationHistoryDto>> GetDocumentVerificationHistoryAsync(int documentId)
        {
            const string sql = @"
            SELECT Id, UserDocumentId, PreviousStatus, NewStatus,
                   Notes, VerifiedBy, VerifiedDate, CreatedDate
            FROM SurveyBucks.DocumentVerificationHistory
            WHERE UserDocumentId = @DocumentId
            ORDER BY VerifiedDate DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<DocumentVerificationHistoryDto>(sql, new { DocumentId = documentId });
        }

        public async Task<string> GetDocumentCurrentStatusAsync(int documentId)
        {
            const string sql = @"
            SELECT VerificationStatus
            FROM SurveyBucks.UserDocument
            WHERE Id = @Id AND IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { Id = documentId });
        }

        public async Task<IEnumerable<UserDocumentDto>> GetDocumentsByTypeAndStatusAsync(string userId, int documentTypeId, string status)
        {
            const string sql = @"
            SELECT 
                ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                dt.IsRequired, ud.StoragePath
            FROM SurveyBucks.UserDocument ud
            JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
            WHERE ud.UserId = @UserId 
              AND ud.DocumentTypeId = @DocumentTypeId
              AND ud.VerificationStatus = @Status
              AND ud.IsDeleted = 0
            ORDER BY ud.UploadedDate DESC";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserDocumentDto>(sql, new
            {
                UserId = userId,
                DocumentTypeId = documentTypeId,
                Status = status
            });
        }

        public async Task<UserDocumentSummaryDto> GetUserDocumentSummaryAsync(string userId)
        {
            const string sql = @"
            SELECT 
                COUNT(*) AS TotalDocuments,
                SUM(CASE WHEN ud.VerificationStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedDocuments,
                SUM(CASE WHEN ud.VerificationStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingDocuments,
                SUM(CASE WHEN ud.VerificationStatus = 'Rejected' THEN 1 ELSE 0 END) AS RejectedDocuments,
                SUM(CASE WHEN dt.IsRequired = 1 THEN 1 ELSE 0 END) AS RequiredDocuments,
                SUM(CASE WHEN dt.IsRequired = 1 AND ud.VerificationStatus = 'Approved' THEN 1 ELSE 0 END) AS RequiredApproved
            FROM SurveyBucks.UserDocument ud
            JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
            WHERE ud.UserId = @UserId AND ud.IsDeleted = 0";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserDocumentSummaryDto>(sql, new { UserId = userId })
                   ?? new UserDocumentSummaryDto { UserId = userId };
        }

        #region Admin Methods

        public async Task<IEnumerable<UserDocumentDto>> GetPendingDocumentsAsync(int? documentTypeId = null, int pageSize = 50, int pageNumber = 1)
        {
            var offset = (pageNumber - 1) * pageSize;
            var whereClause = documentTypeId.HasValue ? "AND ud.DocumentTypeId = @DocumentTypeId" : "";

            var sql = $@"
                SELECT 
                    ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                    dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                    ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                    ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                    dt.IsRequired, ud.StoragePath,
                    u.Email, u.FirstName, u.LastName
                FROM SurveyBucks.UserDocument ud
                INNER JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                INNER JOIN SurveyBucks.Users u ON ud.UserId = u.Id
                WHERE ud.VerificationStatus = 'Pending' 
                  AND ud.IsDeleted = 0
                  {whereClause}
                ORDER BY ud.UploadedDate ASC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using var connection = _connectionFactory.CreateConnection();
            
            var parameters = new Dictionary<string, object>
            {
                { "Offset", offset },
                { "PageSize", pageSize }
            };
            
            if (documentTypeId.HasValue)
            {
                parameters.Add("DocumentTypeId", documentTypeId.Value);
            }
            
            return await connection.QueryAsync<UserDocumentDto>(sql, parameters);
        }

        public async Task<IEnumerable<UserDocumentDto>> GetDocumentsByStatusAsync(string status, int? documentTypeId = null, int pageSize = 50, int pageNumber = 1)
        {
            var offset = (pageNumber - 1) * pageSize;
            var whereClause = documentTypeId.HasValue ? "AND ud.DocumentTypeId = @DocumentTypeId" : "";

            var sql = $@"
                SELECT 
                    ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                    dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                    ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                    ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                    dt.IsRequired, ud.StoragePath,
                    u.Email, u.FirstName, u.LastName
                FROM SurveyBucks.UserDocument ud
                JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                JOIN AspNetUsers u ON ud.UserId = u.Id
                WHERE ud.VerificationStatus = @Status
                  AND ud.IsDeleted = 0
                  {whereClause}
                ORDER BY ud.UploadedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserDocumentDto>(sql, new
            {
                Status = status,
                DocumentTypeId = documentTypeId,
                Offset = offset,
                PageSize = pageSize
            });
        }

        public async Task<int> GetPendingDocumentsCountAsync(int? documentTypeId = null)
        {
            var whereClause = documentTypeId.HasValue ? "AND DocumentTypeId = @DocumentTypeId" : "";

            var sql = $@"
                SELECT COUNT(*)
                FROM SurveyBucks.UserDocument
                WHERE VerificationStatus = 'Pending' 
                  AND IsDeleted = 0
                  {whereClause}";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new { DocumentTypeId = documentTypeId });
        }

        public async Task<AdminDocumentStatsDto> GetDocumentVerificationStatsAsync()
        {
            // Return basic stats - tables may not exist yet
            var stats = new AdminDocumentStatsDto
            {
                TotalDocuments = 0,
                PendingDocuments = 0,
                ApprovedDocuments = 0,
                RejectedDocuments = 0,
                DocumentsToday = 0,
                DocumentsThisWeek = 0,
                DocumentsThisMonth = 0
            };

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                
                // Simple query to test table existence
                const string sql = @"
                    SELECT 
                        COUNT(*) AS TotalDocuments,
                        SUM(CASE WHEN VerificationStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingDocuments,
                        SUM(CASE WHEN VerificationStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedDocuments,
                        SUM(CASE WHEN VerificationStatus = 'Rejected' THEN 1 ELSE 0 END) AS RejectedDocuments,
                        0 AS DocumentsToday,
                        0 AS DocumentsThisWeek,
                        0 AS DocumentsThisMonth
                    FROM SurveyBucks.UserDocument
                    WHERE IsDeleted = 0";
                
                stats = await connection.QuerySingleAsync<AdminDocumentStatsDto>(sql);
            }
            catch (Exception ex)
            {
                // Log the specific error and return default stats
                Console.WriteLine($"Document stats query failed: {ex.Message}");
            }
            
            return stats;
        }

        public async Task<IEnumerable<UserDocumentDto>> SearchDocumentsAsync(string searchTerm, string status = null, int pageSize = 50, int pageNumber = 1)
        {
            var offset = (pageNumber - 1) * pageSize;
            var statusClause = !string.IsNullOrEmpty(status) ? "AND ud.VerificationStatus = @Status" : "";

            var sql = $@"
                SELECT 
                    ud.Id, ud.UserId, ud.DocumentTypeId, dt.Name AS DocumentTypeName,
                    dt.Category, ud.FileName, ud.OriginalFileName, ud.FileSize,
                    ud.ContentType, ud.VerificationStatus, ud.VerificationNotes,
                    ud.VerifiedDate, ud.VerifiedBy, ud.ExpiryDate, ud.UploadedDate,
                    dt.IsRequired, ud.StoragePath,
                    u.Email, u.FirstName, u.LastName
                FROM SurveyBucks.UserDocument ud
                JOIN SurveyBucks.DocumentType dt ON ud.DocumentTypeId = dt.Id
                JOIN AspNetUsers u ON ud.UserId = u.Id
                WHERE ud.IsDeleted = 0
                  {statusClause}
                  AND (
                      u.Email LIKE @SearchTerm OR 
                      u.FirstName LIKE @SearchTerm OR 
                      u.LastName LIKE @SearchTerm OR
                      dt.Name LIKE @SearchTerm OR
                      ud.OriginalFileName LIKE @SearchTerm
                  )
                ORDER BY ud.UploadedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<UserDocumentDto>(sql, new
            {
                SearchTerm = $"%{searchTerm}%",
                Status = status,
                Offset = offset,
                PageSize = pageSize
            });
        }

        #endregion

        public async Task<IEnumerable<DocumentTypeDto>> GetMissingRequiredDocumentTypesAsync(string userId)
        {
            const string sql = @"
            SELECT dt.Id, dt.Name, dt.Description, dt.Category, dt.IsRequired,
                   dt.MaxFileSizeMB, dt.AllowedFileTypes, dt.IsActive, dt.DisplayOrder
            FROM SurveyBucks.DocumentType dt
            WHERE dt.IsRequired = 1 
              AND dt.IsActive = 1 
              AND dt.IsDeleted = 0
              AND NOT EXISTS (
                  SELECT 1 FROM SurveyBucks.UserDocument ud 
                  WHERE ud.DocumentTypeId = dt.Id 
                    AND ud.UserId = @UserId 
                    AND ud.VerificationStatus IN ('Approved', 'Pending')
                    AND ud.IsDeleted = 0
              )
            ORDER BY dt.DisplayOrder";

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<DocumentTypeDto>(sql, new { UserId = userId });
        }
    }
}
