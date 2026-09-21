namespace GiveAid.Web.Models.Entities;

public class Programme
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string? Location { get; set; }
    public string? ImagePath { get; set; }
    public string Status { get; set; } = "Published";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ProgrammeInterest> Interests { get; set; } = new List<ProgrammeInterest>();
    public virtual ICollection<GalleryImage> GalleryImages { get; set; } = new List<GalleryImage>();
}
