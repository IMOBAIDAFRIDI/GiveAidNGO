namespace GiveAid.Web.Models.Entities;

public class ContactInfo
{
    public int Id { get; set; }
    public string Label { get; set; } = "Headquarters";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MapUrl { get; set; }
    public string? SocialLinksJson { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
