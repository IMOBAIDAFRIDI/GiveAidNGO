namespace GiveAid.Web.Models.Entities;

public class Invitation
{
    public int Id { get; set; }
    public int InviterUserId { get; set; }
    public virtual ApplicationUser InviterUser { get; set; } = null!;

    public string RecipientEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = "Pending";

    public DateTime? SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
