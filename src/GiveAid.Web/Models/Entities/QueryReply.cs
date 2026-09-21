namespace GiveAid.Web.Models.Entities;

public class QueryReply
{
    public int Id { get; set; }
    public int QueryId { get; set; }
    public virtual Query Query { get; set; } = null!;

    public int AdminUserId { get; set; }
    public virtual ApplicationUser AdminUser { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public DateTime RepliedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
