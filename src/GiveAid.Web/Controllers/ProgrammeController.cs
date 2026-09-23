using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Programmes;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

[Route("programmes")]
[Route("programme")]
public class ProgrammeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProgrammeService _programmeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProgrammeController(
        ApplicationDbContext context,
        IProgrammeService programmeService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _programmeService = programmeService;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? category = null, string? search = null)
    {
        var query = _context.Programmes
            .Where(p => p.Status == ProgrammeStatuses.Published)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search) || (p.Location != null && p.Location.Contains(search)));
        }

        var programmes = await query.OrderBy(p => p.StartAt).ToListAsync();

        var categories = await _context.Programmes
            .Where(p => p.Status == ProgrammeStatuses.Published && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category;
        ViewBag.SearchTerm = search;

        return View(programmes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var programme = await _context.Programmes
            .Include(p => p.GalleryImages)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProgrammeStatuses.Published);

        if (programme == null) return NotFound();

        bool isUserInterested = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                isUserInterested = await _programmeService.IsUserInterestedAsync(programme.Id, user.Id);
            }
        }

        var totalInterests = await _programmeService.GetInterestsCountAsync(programme.Id);

        var viewModel = new ProgrammeDetailsViewModel
        {
            Programme = programme,
            IsUserInterested = isUserInterested,
            TotalInterestsCount = totalInterests,
            IsAuthenticated = User.Identity?.IsAuthenticated == true
        };

        return View(viewModel);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> DetailsBySlug(string slug)
    {
        var programme = await _context.Programmes
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProgrammeStatuses.Published);

        if (programme == null) return NotFound();
        return RedirectToAction(nameof(Details), new { id = programme.Id });
    }
}
