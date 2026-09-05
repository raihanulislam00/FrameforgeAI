using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoMaker.Application.Common.Interfaces;

namespace VideoMaker.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        var configPath = configuration["STORAGE_PATH"];
        if (string.IsNullOrWhiteSpace(configPath))
        {
            // Default to storage directory relative to solution/app root
            _baseStoragePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "storage"));
        }
        else
        {
            _baseStoragePath = Path.GetFullPath(configPath);
        }

        Directory.CreateDirectory(Path.Combine(_baseStoragePath, "videos"));
        Directory.CreateDirectory(Path.Combine(_baseStoragePath, "thumbnails"));
        Directory.CreateDirectory(Path.Combine(_baseStoragePath, "assets"));
    }

    public string BaseStoragePath => _baseStoragePath;

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string category, CancellationToken cancellationToken = default)
    {
        var categoryDir = Path.Combine(_baseStoragePath, category.ToLowerInvariant());
        Directory.CreateDirectory(categoryDir);

        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(categoryDir, safeFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Stored file at {FullPath}", fullPath);
        return $"/storage/{category.ToLowerInvariant()}/{safeFileName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        try
        {
            var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (relativePath.StartsWith("storage" + Path.DirectorySeparatorChar))
            {
                relativePath = relativePath.Substring("storage".Length + 1);
            }

            var fullPath = Path.Combine(_baseStoragePath, relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file {FullPath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file for URL {FileUrl}", fileUrl);
        }

        return Task.CompletedTask;
    }

    public Task<string> GetUrlAsync(string relativePath)
    {
        return Task.FromResult(relativePath);
    }
}
