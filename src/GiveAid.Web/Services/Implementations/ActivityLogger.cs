using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Services.Interfaces;

namespace GiveAid.Web.Services.Implementations;

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _context;

    public ActivityLogger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int adminUserId, string action, string entityType, int? entityId = null, string? metadataJson = null, string? ipAddress = null)
    {
        var log = new AdminActivityLog
        {
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = metadataJson,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AdminActivityLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
