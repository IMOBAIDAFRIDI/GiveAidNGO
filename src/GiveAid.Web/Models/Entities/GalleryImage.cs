namespace GiveAid.Web.Models.Entities;

public class GalleryImage
{
    public int Id { get; set; }
    public int? ProgrammeId { get; set; }
    public virtual Programme? Programme { get; set; }

    public string? Title { get; set; }
    public string? Caption { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public int UploadedByUserId { get; set; }
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
