using Domain.Interfaces.Repository;
using Domain.Interfaces.Service;
using Domain.Models;
using Domain.Models.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;
        private readonly IGamificationService _gamificationService;
        private readonly IUserProfileCompletionService _profileCompletionService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            INotificationService notificationService,
            IGamificationService gamificationService,
            IUserProfileCompletionService profileCompletionService,
            ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
            _gamificationService = gamificationService;
            _profileCompletionService = profileCompletionService;
            _logger = logger;
        }

        public async Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync()
        {
            return await _documentRepository.GetDocumentTypesAsync();
        }

        public async Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(string userId)
        {
            return await _documentRepository.GetUserDocumentsAsync(userId);
        }

        public async Task<UserDocumentDto> GetUserDocumentByIdAsync(int id, string userId)
        {
            var document = await _documentRepository.GetUserDocumentByIdAsync(id);

            if (document == null)
            {
                throw new NotFoundException($"Document with ID {id} not found");
            }

            if (document.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only access your own documents");
            }

            return document;
        }

        public async Task<DocumentUploadResultDto> UploadDocumentAsync(string userId, DocumentUploadRequestDto uploadRequest)
        {
            _logger.LogInformation("User {UserId} uploading document type {DocumentTypeId}", userId, uploadRequest.DocumentTypeId);

            try
            {
                // 1. BUSINESS VALIDATION - Get and validate document type
                var documentType = await _documentRepository.GetDocumentTypeByIdAsync(uploadRequest.DocumentTypeId);
                if (documentType == null || !documentType.IsActive)
                {
                    return new DocumentUploadResultDto
                    {
                        Success = false,
                        ErrorMessage = "Invalid or inactive document type"
                    };
                }

                // 2. BUSINESS VALIDATION - File validation
                var fileValidation = ValidateUploadedFile(uploadRequest.File, documentType);
                if (!fileValidation.IsValid)
                {
                    return new DocumentUploadResultDto
                    {
                        Success = false,
                        ErrorMessage = fileValidation.ErrorMessage
                    };
                }

                // 3. BUSINESS VALIDATION - Check for existing documents
                var existingValidation = await ValidateExistingDocumentsAsync(userId, documentType);
                if (!existingValidation.IsValid)
                {
                    return new DocumentUploadResultDto
                    {
                        Success = false,
                        ErrorMessage = existingValidation.ErrorMessage
                    };
                }

                // 4. CORE OPERATION - Upload file to local storage
                var uploadResult = await UploadFileToStorageAsync(uploadRequest.File, userId, documentType);
                if (!uploadResult.Success)
                {
                    return new DocumentUploadResultDto
                    {
                        Success = false,
                        ErrorMessage = uploadResult.ErrorMessage
                    };
                }

                // 5. CORE OPERATION - Create document record
                var document = new UserDocumentDto
                {
                    UserId = userId,
                    DocumentTypeId = uploadRequest.DocumentTypeId,
                    FileName = uploadResult.FileName,
                    OriginalFileName = uploadRequest.File.FileName,
                    FileSize = uploadRequest.File.Length,
                    ContentType = uploadRequest.File.ContentType,
                    StoragePath = uploadResult.StoragePath,
                    VerificationStatus = "Pending",
                    IsEncrypted = false,
                    UploadedDate = DateTimeOffset.UtcNow,
                    ExpiryDate = uploadRequest.ExpiryDate
                };

                var documentId = await _documentRepository.CreateUserDocumentAsync(document);
                document.Id = documentId;

                // 6. FIRE-AND-FORGET BUSINESS OPERATIONS
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessDocumentUploadGamificationAsync(userId, documentType);
                        await SendDocumentUploadNotificationAsync(userId, documentType.Name);
                        await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Documents");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process document upload background operations for user {UserId}", userId);
                    }
                });

                _logger.LogInformation("User {UserId} successfully uploaded document {DocumentId}", userId, documentId);

                return new DocumentUploadResultDto
                {
                    Success = true,
                    DocumentId = documentId,
                    FileName = uploadResult.FileName,
                    Message = $"Your {documentType.Name} has been uploaded successfully and is pending verification."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user {UserId}", userId);
                return new DocumentUploadResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred while uploading your document. Please try again."
                };
            }
        }

        private async Task<FileUploadResult> UploadFileToStorageAsync(IFormFile file, string userId, DocumentTypeDto documentType)
        {
            try
            {
                // Generate secure filename
                var fileExtension = Path.GetExtension(file.FileName);
                var secureFileName = $"{Guid.NewGuid()}{fileExtension}";

                // Create folder structure: documents/{userId}/{category}
                var folderPath = Path.Combine("documents", userId, documentType.Category.ToLowerInvariant());

                using var fileStream = file.OpenReadStream();
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    fileStream,
                    secureFileName,
                    file.ContentType,
                    folderPath);

                if (!uploadResult.Success)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        ErrorMessage = uploadResult.ErrorMessage ?? "Failed to upload file to storage"
                    };
                }

                return new FileUploadResult
                {
                    Success = true,
                    FileName = secureFileName,
                    StoragePath = uploadResult.StoragePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to storage for user {UserId}", userId);
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to save file to storage"
                };
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId, string userId)
        {
            _logger.LogInformation("User {UserId} deleting document {DocumentId}", userId, documentId);

            try
            {
                // 1. BUSINESS VALIDATION - Get document and validate ownership
                var document = await _documentRepository.GetUserDocumentByIdAsync(documentId);
                if (document == null || document.UserId != userId)
                {
                    throw new UnauthorizedAccessException("Document not found or access denied");
                }

                // 2. BUSINESS VALIDATION - Check if document can be deleted
                if (document.VerificationStatus == "Approved")
                {
                    // Don't allow deletion of approved documents - user should upload a new one instead
                    throw new InvalidOperationException("Cannot delete an approved document. Upload a new document to replace it.");
                }

                // 3. CORE OPERATION - Delete from storage
                var storageDeleteResult = await _fileStorageService.DeleteFileAsync(document.StoragePath);
                if (!storageDeleteResult)
                {
                    _logger.LogWarning("Failed to delete file from storage: {StoragePath}", document.StoragePath);
                    // Continue anyway - we'll mark as deleted in database
                }

                // 4. CORE OPERATION - Mark as deleted in database
                var result = await _documentRepository.DeleteUserDocumentAsync(documentId, userId);

                if (result)
                {
                    // 5. FIRE-AND-FORGET BUSINESS OPERATIONS
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SendDocumentDeletionNotificationAsync(userId, document.DocumentTypeName);
                            await _profileCompletionService.ProcessProfileUpdateAsync(userId, "Documents");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process document deletion background operations for user {UserId}", userId);
                        }
                    });

                    _logger.LogInformation("User {UserId} successfully deleted document {DocumentId}", userId, documentId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} for user {UserId}", documentId, userId);
                throw;
            }
        }

        public async Task<UserVerificationStatusDto> GetUserVerificationStatusAsync(string userId)
        {
            return await _documentRepository.GetUserVerificationStatusAsync(userId);
        }

        public async Task<Stream> DownloadDocumentAsync(int documentId, string userId)
        {
            var document = await GetUserDocumentByIdAsync(documentId, userId);
            return await _fileStorageService.DownloadFileAsync(document.FileName);
        }

        public async Task<DocumentVerificationResultDto> VerifyDocumentAsync(int documentId, string status, string notes, string verifiedBy)
        {
            _logger.LogInformation("Verifying document {DocumentId} with status {Status} by {VerifiedBy}", documentId, status, verifiedBy);

            try
            {
                // 1. BUSINESS VALIDATION - Get document and validate
                var document = await _documentRepository.GetUserDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return new DocumentVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Document not found"
                    };
                }

                // 2. BUSINESS VALIDATION - Check if already verified
                if (document.VerificationStatus == "Approved")
                {
                    return new DocumentVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Document is already verified"
                    };
                }

                // 3. BUSINESS VALIDATION - Validate status transition
                if (!IsValidStatusTransition(document.VerificationStatus, status))
                {
                    return new DocumentVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Invalid status transition from {document.VerificationStatus} to {status}"
                    };
                }

                // 4. CORE OPERATION - Update document status
                var previousStatus = document.VerificationStatus;
                var updateResult = await _documentRepository.UpdateDocumentStatusAsync(documentId, status, notes, verifiedBy);
                if (!updateResult)
                {
                    return new DocumentVerificationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Failed to update document status"
                    };
                }

                // 5. CORE OPERATION - Create verification history
                var historyRecord = new DocumentVerificationHistoryDto
                {
                    UserDocumentId = documentId,
                    PreviousStatus = previousStatus,
                    NewStatus = status,
                    Notes = notes,
                    VerifiedBy = verifiedBy,
                    VerifiedDate = DateTimeOffset.UtcNow
                };

                await _documentRepository.CreateVerificationHistoryAsync(historyRecord);

                // 6. IMMEDIATE PROFILE UPDATE - Don't run in background to ensure completion
                try
                {
                    await _profileCompletionService.ProcessProfileUpdateAsync(document.UserId, "Documents");
                    _logger.LogInformation("Profile completion updated for user {UserId} after document verification", document.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update profile completion for document {DocumentId}", documentId);
                }

                // 7. FIRE-AND-FORGET BUSINESS OPERATIONS  
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessDocumentVerificationGamificationAsync(document.UserId, status, document.DocumentTypeName);
                        await SendDocumentVerificationNotificationAsync(document.UserId, document.DocumentTypeName, status, notes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process document verification background operations for document {DocumentId}", documentId);
                    }
                });

                _logger.LogInformation("Document {DocumentId} verified with status {Status}", documentId, status);

                return new DocumentVerificationResultDto
                {
                    Success = true,
                    DocumentId = documentId,
                    NewStatus = status,
                    Message = GetVerificationMessage(status, document.DocumentTypeName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document {DocumentId}", documentId);
                return new DocumentVerificationResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing the verification"
                };
            }
        }

        public async Task<IEnumerable<DocumentVerificationHistoryDto>> GetDocumentHistoryAsync(int documentId)
        {
            return await _documentRepository.GetDocumentVerificationHistoryAsync(documentId);
        }

        #region Admin Document Management

        public async Task<IEnumerable<UserDocumentDto>> GetPendingDocumentsAsync(int? documentTypeId = null, int pageSize = 50, int pageNumber = 1)
        {
            _logger.LogDebug("Getting pending documents with type filter: {DocumentTypeId}, page: {PageNumber}, size: {PageSize}", documentTypeId, pageNumber, pageSize);
            return await _documentRepository.GetPendingDocumentsAsync(documentTypeId, pageSize, pageNumber);
        }

        public async Task<IEnumerable<UserDocumentDto>> GetDocumentsByStatusAsync(string status, int? documentTypeId = null, int pageSize = 50, int pageNumber = 1)
        {
            _logger.LogDebug("Getting documents by status: {Status}, type filter: {DocumentTypeId}, page: {PageNumber}, size: {PageSize}", status, documentTypeId, pageNumber, pageSize);
            return await _documentRepository.GetDocumentsByStatusAsync(status, documentTypeId, pageSize, pageNumber);
        }

        public async Task<int> GetPendingDocumentsCountAsync(int? documentTypeId = null)
        {
            return await _documentRepository.GetPendingDocumentsCountAsync(documentTypeId);
        }

        public async Task<AdminDocumentStatsDto> GetDocumentVerificationStatsAsync()
        {
            _logger.LogDebug("Getting document verification statistics");
            return await _documentRepository.GetDocumentVerificationStatsAsync();
        }

        public async Task<IEnumerable<UserDocumentDto>> SearchDocumentsAsync(string searchTerm, string status = null, int pageSize = 50, int pageNumber = 1)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));
            }

            _logger.LogDebug("Searching documents with term: {SearchTerm}, status: {Status}, page: {PageNumber}, size: {PageSize}", searchTerm, status, pageNumber, pageSize);
            return await _documentRepository.SearchDocumentsAsync(searchTerm, status, pageSize, pageNumber);
        }

        public async Task<(Stream FileStream, string FileName, string ContentType)> DownloadDocumentForAdminAsync(int documentId)
        {
            _logger.LogDebug("Admin downloading document {DocumentId}", documentId);
            
            var document = await _documentRepository.GetUserDocumentByIdAsync(documentId);
            if (document == null)
            {
                throw new NotFoundException($"Document with ID {documentId} not found");
            }

            var fileStream = await _fileStorageService.DownloadFileAsync(document.StoragePath);
            return (fileStream, document.OriginalFileName, document.ContentType);
        }

        #endregion

        private FileValidationResult ValidateUploadedFile(IFormFile file, DocumentTypeDto documentType)
        {
            if (file == null || file.Length == 0)
            {
                return new FileValidationResult { IsValid = false, ErrorMessage = "No file uploaded" };
            }

            // Check file size (convert MB to bytes)
            var maxSizeBytes = documentType.MaxFileSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size ({file.Length / 1024 / 1024} MB) exceeds maximum allowed size ({documentType.MaxFileSizeMB} MB)"
                };
            }

            // Check file extension
            var allowedExtensions = documentType.AllowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLowerInvariant())
                .ToArray();

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File type '{fileExtension}' is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}"
                };
            }

            // Basic file content validation (check file signature)
            var contentValidation = _fileStorageService.ValidateFileContent(file);
            if (!contentValidation)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid file content or potential security issue detected"
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        private async Task<BusinessValidationResult> ValidateExistingDocumentsAsync(string userId, DocumentTypeDto documentType)
        {
            // For identity documents, only allow one active document per type
            if (documentType.Category == "Identity")
            {
                var existingDocs = await _documentRepository.GetDocumentsByTypeAndStatusAsync(
                    userId, documentType.Id, "Approved");

                if (existingDocs.Any())
                {
                    return new BusinessValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"You already have an approved {documentType.Name} document. Please contact support if you need to update it."
                    };
                }

                // Check for pending documents
                var pendingDocs = await _documentRepository.GetDocumentsByTypeAndStatusAsync(
                    userId, documentType.Id, "Pending");

                if (pendingDocs.Any())
                {
                    return new BusinessValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"You already have a {documentType.Name} document pending verification. Please wait for the current document to be processed."
                    };
                }
            }

            return new BusinessValidationResult { IsValid = true };
        }

        // BUSINESS LOGIC - Status transition validation
        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                ("Pending", "Approved") => true,
                ("Pending", "Rejected") => true,
                ("Rejected", "Pending") => true, // Allow re-submission after rejection
                ("Rejected", "Approved") => true, // Direct approval after rejection
                _ => false
            };
        }

        // BUSINESS LOGIC - Gamification processing
        private async Task ProcessDocumentUploadGamificationAsync(string userId, DocumentTypeDto documentType)
        {
            // Award points for document upload
            var points = GetDocumentUploadPoints(documentType);
            await _gamificationService.ProcessPointsEarnedAsync(userId, points, "DocumentUpload", documentType.Id.ToString());

            // Check for upload achievements
            await _gamificationService.ProcessDocumentUploadAsync(userId, documentType.Category);
        }

        private async Task ProcessDocumentVerificationGamificationAsync(string userId, string status, string documentTypeName)
        {
            if (status == "Approved")
            {
                // Award points for successful verification
                var points = GetDocumentVerificationPoints(documentTypeName);
                await _gamificationService.ProcessPointsEarnedAsync(userId, points, "DocumentVerification", documentTypeName);

                // Process verification achievements
                await _gamificationService.ProcessDocumentVerificationAsync(userId, documentTypeName);
            }
        }

        // BUSINESS LOGIC - Notification processing
        private async Task SendDocumentUploadNotificationAsync(string userId, string documentTypeName)
        {
            await _notificationService.SendDocumentUploadedNotificationAsync(userId, documentTypeName);
        }

        private async Task SendDocumentVerificationNotificationAsync(string userId, string documentTypeName, string status, string notes)
        {
            if (status == "Approved")
            {
                await _notificationService.SendDocumentApprovedNotificationAsync(userId, documentTypeName);
            }
            else if (status == "Rejected")
            {
                await _notificationService.SendDocumentRejectedNotificationAsync(userId, documentTypeName, notes);
            }
        }

        private async Task SendDocumentDeletionNotificationAsync(string userId, string documentTypeName)
        {
            await _notificationService.SendDocumentDeletedNotificationAsync(userId, documentTypeName);
        }

        // BUSINESS LOGIC - Helper methods
        private int GetDocumentUploadPoints(DocumentTypeDto documentType)
        {
            return documentType.Category switch
            {
                "Identity" => 25, // Higher points for identity documents
                "Address" => 20,
                "Income" => 15,
                _ => 10
            };
        }

        private int GetDocumentVerificationPoints(string documentTypeName)
        {
            return documentTypeName.ToLowerInvariant() switch
            {
                var name when name.Contains("id") || name.Contains("passport") => 100,
                var name when name.Contains("address") || name.Contains("utility") => 75,
                var name when name.Contains("income") || name.Contains("salary") => 50,
                _ => 25
            };
        }

        private string GetVerificationMessage(string status, string documentTypeName)
        {
            return status switch
            {
                "Approved" => $"Great! Your {documentTypeName} has been approved. You can now access all features that require this verification.",
                "Rejected" => $"Your {documentTypeName} could not be verified. Please check the notes and upload a new document.",
                _ => $"Your {documentTypeName} verification status has been updated to {status}."
            };
        }
    }
}
