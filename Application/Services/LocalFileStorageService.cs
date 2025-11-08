using Domain.Interfaces.Service;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _basePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _logger = logger;

            // Ensure base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<FileUploadResultDto> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder)
        {
            try
            {
                // Generate unique file name
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var folderPath = Path.Combine(_basePath, folder);

                // Ensure folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, uniqueFileName);

                // Save file
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                return new FileUploadResultDto
                {
                    Success = true,
                    FileName = uniqueFileName,
                    StoragePath = Path.Combine(folder, uniqueFileName),
                    FileSize = new FileInfo(filePath).Length,
                    ContentType = contentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                return new FileUploadResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to upload file"
                };
            }
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string filePath)
        {
            // For local storage, return a relative URL that can be served by the API
            return await Task.FromResult($"/api/documents/download/{filePath}");
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

            // Additional security check for file content
            // This is a basic check - you might want to implement more sophisticated validation
            var fileSignature = GetFileSignature(file);
            if (!IsValidFileSignature(extension, fileSignature))
            {
                return false;
            }

            return true;
        }

        private byte[] GetFileSignature(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var signature = new byte[Math.Min(file.Length, 10)];
                    stream.Read(signature, 0, signature.Length);
                    stream.Position = 0; // Reset position for future reads
                    return signature;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file signature for {FileName}", file.FileName);
                return new byte[0];
            }
        }

        public bool ValidateFileContent(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File is null or empty during content validation");
                    return false;
                }

                // Get file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');

                // Get file signature to verify file content matches extension
                var fileSignature = GetFileSignature(file);

                // Validate that the file signature matches the expected file type
                var isValidSignature = IsValidFileSignature(extension, fileSignature);

                if (!isValidSignature)
                {
                    _logger.LogWarning("File signature validation failed for file {FileName} with extension {Extension}",
                        file.FileName, extension);
                    return false;
                }

                // Additional content validation checks
                if (!ValidateFileStructure(file, extension))
                {
                    _logger.LogWarning("File structure validation failed for file {FileName}", file.FileName);
                    return false;
                }

                // Check for potentially malicious content
                if (ContainsSuspiciousContent(file))
                {
                    _logger.LogWarning("File contains suspicious content: {FileName}", file.FileName);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file content validation for {FileName}", file.FileName);
                return false;
            }
        }

        private bool IsValidFileSignature(string extension, byte[] signature)
        {
            if (signature == null || signature.Length == 0)
            {
                return false;
            }

            // File signatures for common formats
            var signatures = new Dictionary<string, List<byte[]>>
            {
                ["jpg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
                ["jpeg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
                ["png"] = new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
                ["pdf"] = new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
                ["gif"] = new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
                ["bmp"] = new List<byte[]> { new byte[] { 0x42, 0x4D } },
                ["doc"] = new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
                ["docx"] = new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
                ["xls"] = new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
                ["xlsx"] = new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
                ["zip"] = new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
                ["txt"] = new List<byte[]>() // Text files don't have a specific signature
            };

            // If we don't have a signature to check, allow it (for text files, etc.)
            if (!signatures.ContainsKey(extension))
            {
                _logger.LogDebug("No signature validation available for extension: {Extension}", extension);
                return true;
            }

            // For text files, allow any content
            if (extension == "txt")
            {
                return true;
            }

            // Check if the file signature matches any of the expected signatures
            var expectedSignatures = signatures[extension];
            foreach (var expectedSignature in expectedSignatures)
            {
                if (signature.Length >= expectedSignature.Length &&
                    signature.Take(expectedSignature.Length).SequenceEqual(expectedSignature))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateFileStructure(IFormFile file, string extension)
        {
            try
            {
                // Basic file structure validation based on extension
                switch (extension.ToLowerInvariant())
                {
                    case "pdf":
                        return ValidatePdfStructure(file);
                    case "jpg":
                    case "jpeg":
                    case "png":
                    case "gif":
                    case "bmp":
                        return ValidateImageStructure(file);
                    case "txt":
                        return ValidateTextStructure(file);
                    default:
                        // For unknown file types, assume valid
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file structure for {FileName}", file.FileName);
                return false;
            }
        }

        private bool ValidatePdfStructure(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // Basic PDF validation - check for PDF header and footer
                    var buffer = new byte[8];
                    stream.Read(buffer, 0, 8);

                    // PDF should start with "%PDF-"
                    var pdfHeader = System.Text.Encoding.ASCII.GetString(buffer, 0, 5);
                    if (pdfHeader != "%PDF-")
                    {
                        return false;
                    }

                    // Check file is not truncated by looking for EOF marker
                    if (stream.Length > 1024)
                    {
                        stream.Seek(-1024, SeekOrigin.End);
                        var endBuffer = new byte[1024];
                        stream.Read(endBuffer, 0, 1024);
                        var endContent = System.Text.Encoding.ASCII.GetString(endBuffer);

                        // PDF should contain %%EOF somewhere near the end
                        if (!endContent.Contains("%%EOF"))
                        {
                            _logger.LogWarning("PDF file appears to be truncated or corrupted: {FileName}", file.FileName);
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating PDF structure for {FileName}", file.FileName);
                return false;
            }
        }

        private bool ValidateImageStructure(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // Basic validation - just check if we can read the first few bytes
                    // More sophisticated validation could use image libraries
                    var buffer = new byte[100];
                    var bytesRead = stream.Read(buffer, 0, 100);

                    return bytesRead > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image structure for {FileName}", file.FileName);
                return false;
            }
        }

        private bool ValidateTextStructure(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    // Read first few characters to ensure it's readable text
                    var buffer = new char[1000];
                    var charsRead = reader.Read(buffer, 0, 1000);

                    if (charsRead == 0)
                    {
                        return false;
                    }

                    // Check for null bytes which shouldn't be in text files
                    var text = new string(buffer, 0, charsRead);
                    if (text.Contains('\0'))
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating text structure for {FileName}", file.FileName);
                return false;
            }
        }

        private bool ContainsSuspiciousContent(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // Check for suspicious patterns that might indicate malware or executable content
                    var buffer = new byte[Math.Min(4096, (int)file.Length)];
                    stream.Read(buffer, 0, buffer.Length);

                    // Convert to string for pattern matching
                    var content = System.Text.Encoding.ASCII.GetString(buffer);

                    // Check for executable signatures
                    var suspiciousPatterns = new[]
                    {
                        "MZ", // DOS executable header
                        "\x7fELF", // Linux executable header
                        "PK\x03\x04", // ZIP file (could contain executables, but we allow this for valid docs)
                    };

                    // Check for script patterns that might be dangerous
                    var scriptPatterns = new[]
                    {
                        "<script",
                        "javascript:",
                        "vbscript:",
                        "data:text/html",
                        "<?php",
                        "<%",
                    };

                    // Only flag script patterns as suspicious, not general executable patterns
                    // since we might legitimately have ZIP-based office documents
                    foreach (var pattern in scriptPatterns)
                    {
                        if (content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _logger.LogWarning("Suspicious script pattern found in file: {FileName}", file.FileName);
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for suspicious content in {FileName}", file.FileName);
                // If we can't check, err on the side of caution but don't block the upload
                return false;
            }
        }
    }
}
