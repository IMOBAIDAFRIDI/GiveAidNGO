namespace GiveAid.Web.Models.Entities;

public class Query
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<QueryReply> Replies { get; set; } = new List<QueryReply>();
}
