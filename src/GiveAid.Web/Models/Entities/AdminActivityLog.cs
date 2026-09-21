namespace GiveAid.Web.Models.Entities;

public class AdminActivityLog
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public virtual ApplicationUser AdminUser { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
