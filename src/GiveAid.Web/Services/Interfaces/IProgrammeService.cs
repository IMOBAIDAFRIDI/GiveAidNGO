using GiveAid.Web.Models.Entities;

namespace GiveAid.Web.Services.Interfaces;

public record InterestRegistrationResult(bool Success, bool AlreadyRegistered, string Message);

public interface IProgrammeService
{
    Task<List<Programme>> GetPublishedProgrammesAsync();
    Task<Programme?> GetProgrammeBySlugAsync(string slug);
    Task<Programme?> GetProgrammeByIdAsync(int id);
    Task<InterestRegistrationResult> RegisterInterestAsync(int programmeId, int userId);
    Task<bool> IsUserInterestedAsync(int programmeId, int userId);
    Task<List<ProgrammeInterest>> GetUserInterestsAsync(int userId);
    Task<int> GetInterestsCountAsync(int programmeId);
}
