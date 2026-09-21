namespace GiveAid.Web.Services.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(int adminUserId, string action, string entityType, int? entityId = null, string? metadataJson = null, string? ipAddress = null);
}
