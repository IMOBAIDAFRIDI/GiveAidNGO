using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GiveAid.Web.Services.Implementations;

public class MediaService : IMediaService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MediaService> _logger;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public MediaService(IWebHostEnvironment environment, ILogger<MediaService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string?> UploadImageAsync(IFormFile? file, string folderName)
    {
        if (file == null || file.Length == 0) return null;

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException("Uploaded file exceeds the maximum allowed size of 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file format. Allowed formats: JPG, JPEG, PNG, WEBP.");
        }

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", folderName);

        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var fullPath = Path.Combine(uploadsRoot, safeFileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Successfully uploaded image to {Path}", fullPath);
        return $"/uploads/{folderName}/{safeFileName}";
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, normalizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted old image at {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file at path {RelativePath}", relativePath);
        }
    }
}
