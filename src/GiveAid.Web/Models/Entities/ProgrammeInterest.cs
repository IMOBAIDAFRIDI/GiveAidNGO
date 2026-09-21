namespace GiveAid.Web.Models.Entities;

public class ProgrammeInterest
{
    public int Id { get; set; }
    public int ProgrammeId { get; set; }
    public virtual Programme Programme { get; set; } = null!;

    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public string Status { get; set; } = "Interested";
    public DateTime InterestedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
