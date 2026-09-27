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

    [HttpGet("{slug?}")]
    public async Task<IActionResult> Index(string? slug = null)
    {
        var sections = await _context.AboutSections
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        if (string.Equals(slug, "index", StringComparison.OrdinalIgnoreCase))
        {
            slug = null;
        }

        var activeSlug = !string.IsNullOrWhiteSpace(slug) ? slug.Trim() : (sections.FirstOrDefault()?.Slug ?? "what-we-do");

        // 1. Try matching active section by slug (case-insensitive)
        var current = sections.FirstOrDefault(s => s.Slug.Equals(activeSlug, StringComparison.OrdinalIgnoreCase));

        // 2. Try matching by numeric ID
        if (current == null && int.TryParse(activeSlug, out int id))
        {
            current = sections.FirstOrDefault(s => s.Id == id);
        }

        // 3. Fallback: query database for inactive section if admin wants to preview
        if (current == null && !string.IsNullOrWhiteSpace(activeSlug))
        {
            var isNumeric = int.TryParse(activeSlug, out int fallbackId);
            current = await _context.AboutSections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug.ToLower() == activeSlug.ToLower() || (isNumeric && s.Id == fallbackId));
        }

        // 4. Default to first active section
        current ??= sections.FirstOrDefault();

        var viewModel = new AboutPageViewModel
        {
            ActiveSlug = current?.Slug ?? activeSlug,
            Sections = sections,
            CurrentSection = current
        };

        return View(viewModel);
    }
}
