using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Services.Implementations;

public class ProgrammeService : IProgrammeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProgrammeService> _logger;

    public ProgrammeService(ApplicationDbContext context, ILogger<ProgrammeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Programme>> GetPublishedProgrammesAsync()
    {
        return await _context.Programmes
            .Where(p => p.Status == ProgrammeStatuses.Published)
            .OrderBy(p => p.StartAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Programme?> GetProgrammeBySlugAsync(string slug)
    {
        return await _context.Programmes
            .Include(p => p.Interests)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<Programme?> GetProgrammeByIdAsync(int id)
    {
        return await _context.Programmes
            .Include(p => p.Interests)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<InterestRegistrationResult> RegisterInterestAsync(int programmeId, int userId)
    {
        var programme = await _context.Programmes.FirstOrDefaultAsync(p => p.Id == programmeId);
        if (programme == null || programme.Status != ProgrammeStatuses.Published)
        {
            return new InterestRegistrationResult(false, false, "Programme is not active or available for registration.");
        }

        var existing = await _context.ProgrammeInterests
            .FirstOrDefaultAsync(pi => pi.ProgrammeId == programmeId && pi.UserId == userId);

        if (existing != null)
        {
            return new InterestRegistrationResult(true, true, "You have already registered interest in this programme.");
        }

        try
        {
            var interest = new ProgrammeInterest
            {
                ProgrammeId = programmeId,
                UserId = userId,
                Status = InterestStatuses.Interested,
                InterestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProgrammeInterests.AddAsync(interest);
            await _context.SaveChangesAsync();

            return new InterestRegistrationResult(true, false, "Thank you! Your interest has been registered successfully.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Duplicate interest race condition caught for user {UserId} and programme {ProgrammeId}", userId, programmeId);
            return new InterestRegistrationResult(true, true, "You have already registered interest in this programme.");
        }
    }

    public async Task<bool> IsUserInterestedAsync(int programmeId, int userId)
    {
        return await _context.ProgrammeInterests
            .AnyAsync(pi => pi.ProgrammeId == programmeId && pi.UserId == userId);
    }

    public async Task<List<ProgrammeInterest>> GetUserInterestsAsync(int userId)
    {
        return await _context.ProgrammeInterests
            .Include(pi => pi.Programme)
            .Where(pi => pi.UserId == userId)
            .OrderByDescending(pi => pi.InterestedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetInterestsCountAsync(int programmeId)
    {
        return await _context.ProgrammeInterests
            .CountAsync(pi => pi.ProgrammeId == programmeId);
    }
}
