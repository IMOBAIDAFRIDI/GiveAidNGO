using GiveAid.Web.Data;
using GiveAid.Web.Models.ViewModels.About;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

[Route("about")]
public class AboutController : Controller
{
    private readonly ApplicationDbContext _context;

    public AboutController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string? slug = null)
    {
        var sections = await _context.AboutSections
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        var activeSlug = !string.IsNullOrEmpty(slug) ? slug : "what-we-do";
        var current = sections.FirstOrDefault(s => s.Slug.Equals(activeSlug, StringComparison.OrdinalIgnoreCase))
                      ?? sections.FirstOrDefault();

        var viewModel = new AboutPageViewModel
        {
            ActiveSlug = current?.Slug ?? "what-we-do",
            Sections = sections,
            CurrentSection = current
        };

        return View(viewModel);
    }
}
