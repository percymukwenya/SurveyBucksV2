using Domain.Models;
using Microsoft.AspNetCore.Http;

namespace Domain.Interfaces.Service
{
    public interface IFileStorageService
    {
        Task<FileUploadResultDto> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder);
        Task<Stream> DownloadFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<string> GetFileUrlAsync(string filePath);
        bool ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeMB);
        bool ValidateFileContent(IFormFile file);
    }
}
