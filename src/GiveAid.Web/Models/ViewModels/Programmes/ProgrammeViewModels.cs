using System.ComponentModel.DataAnnotations;
using GiveAid.Web.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace GiveAid.Web.Models.ViewModels.Programmes;

public class ProgrammeCreateViewModel
{
    [Required(ErrorMessage = "Programme title is required.")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100)]
    public string Category { get; set; } = "Education";

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [Display(Name = "Start Date & Time")]
    public DateTime StartAt { get; set; } = DateTime.UtcNow.AddDays(7);

    [Display(Name = "End Date & Time (Optional)")]
    public DateTime? EndAt { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [Display(Name = "Programme Banner Image")]
    public IFormFile? ImageFile { get; set; }

    public string Status { get; set; } = "Published";
}

public class ProgrammeEditViewModel : ProgrammeCreateViewModel
{
    public int Id { get; set; }
    public string? ExistingImagePath { get; set; }
}

public class ProgrammeDetailsViewModel
{
    public Programme Programme { get; set; } = null!;
    public bool IsUserInterested { get; set; }
    public int TotalInterestsCount { get; set; }
    public bool IsAuthenticated { get; set; }
}
