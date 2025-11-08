using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Application.Services
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
            _containerName = configuration["AzureStorage:ContainerName"] ?? "documents";
            _logger = logger;
        }

        public async Task<FileUploadResultDto> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobName = $"{folder}/{Guid.NewGuid()}_{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

                return new FileUploadResultDto
                {
                    Success = true,
                    FileName = fileName,
                    BlobName = blobName,
                    StoragePath = blobClient.Uri.ToString(),
                    FileSize = fileStream.Length,
                    ContentType = contentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Azure Blob Storage");
                return new FileUploadResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to upload file to cloud storage"
                };
            }
        }

        public async Task<Stream> DownloadFileAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException("File not found in cloud storage", blobName);
            }

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<bool> DeleteFileAsync(string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Azure Blob Storage");
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Generate a SAS token for secure access (valid for 1 hour)
            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(
                    Azure.Storage.Sas.BlobSasPermissions.Read,
                    DateTimeOffset.UtcNow.AddHours(1));

                return await Task.FromResult(sasUri.ToString());
            }

            return blobClient.Uri.ToString();
        }

        public bool ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeMB)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            // Check file size
            var maxSizeBytes = maxSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }

            return true;
        }

        public bool ValidateFileContent(IFormFile file)
        {
            throw new NotImplementedException();
        }
    }
}
