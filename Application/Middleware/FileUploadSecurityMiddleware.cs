using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Middleware
{
    public class FileUploadSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FileUploadSecurityMiddleware> _logger;
        private readonly HashSet<string> _allowedContentTypes = new HashSet<string>
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/pdf"
    };

        public FileUploadSecurityMiddleware(RequestDelegate next, ILogger<FileUploadSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                foreach (var file in form.Files)
                {
                    // Validate content type
                    if (!_allowedContentTypes.Contains(file.ContentType.ToLower()))
                    {
                        context.Response.StatusCode = 415; // Unsupported Media Type
                        await context.Response.WriteAsync($"File type {file.ContentType} is not allowed");
                        return;
                    }

                    // Check for double extensions
                    var fileName = file.FileName;
                    if (fileName.Split('.').Length > 2)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Files with multiple extensions are not allowed");
                        return;
                    }

                    // Validate file signature
                    using (var stream = file.OpenReadStream())
                    {
                        if (!await IsValidFileSignature(stream, file.ContentType))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("File content does not match the declared content type");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }

        private async Task<bool> IsValidFileSignature(Stream stream, string contentType)
        {
            var buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Position = 0; // Reset position

            return contentType.ToLower() switch
            {
                "image/jpeg" or "image/jpg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
                "image/png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
                "application/pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
                _ => false
            };
        }
    }
}
