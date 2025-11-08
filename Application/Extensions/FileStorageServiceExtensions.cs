using Application.Services;
using Domain.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Application.Extensions
{
    public static class FileStorageServiceExtensions
    {
        public static IServiceCollection AddFileStorageService(this IServiceCollection services, IConfiguration configuration)
        {
            var storageType = configuration["FileStorage:Type"] ?? "Local";

            if (storageType.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
            }
            else
            {
                services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            }

            return services;
        }
    }
}
