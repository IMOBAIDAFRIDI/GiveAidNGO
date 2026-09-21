using Microsoft.AspNetCore.Identity;

namespace GiveAid.Web.Models.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "Active";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Profession { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();
    public virtual ICollection<ProgrammeInterest> Interests { get; set; } = new List<ProgrammeInterest>();
    public virtual ICollection<Query> Queries { get; set; } = new List<Query>();
    public virtual ICollection<QueryReply> QueryReplies { get; set; } = new List<QueryReply>();
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public virtual ICollection<GalleryImage> UploadedImages { get; set; } = new List<GalleryImage>();
    public virtual ICollection<AdminActivityLog> AdminActivityLogs { get; set; } = new List<AdminActivityLog>();
    public virtual ICollection<AboutSection> UpdatedAboutSections { get; set; } = new List<AboutSection>();
}
