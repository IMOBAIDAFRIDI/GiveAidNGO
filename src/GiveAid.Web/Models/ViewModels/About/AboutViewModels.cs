using System.ComponentModel.DataAnnotations;
using GiveAid.Web.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace GiveAid.Web.Models.ViewModels.About;

public class AboutSectionEditViewModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string? ExistingImagePath { get; set; }
    public IFormFile? ImageFile { get; set; }
}

public class AboutPageViewModel
{
    public string ActiveSlug { get; set; } = "what-we-do";
    public List<AboutSection> Sections { get; set; } = new();
    public AboutSection? CurrentSection { get; set; }
}
