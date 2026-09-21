using Microsoft.AspNetCore.Http;

namespace GiveAid.Web.Services.Interfaces;

public interface IMediaService
{
    Task<string?> UploadImageAsync(IFormFile? file, string folderName);
    void DeleteImage(string? relativePath);
}
