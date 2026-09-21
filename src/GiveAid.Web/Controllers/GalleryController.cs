using GiveAid.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

public class GalleryController : Controller
{
    private readonly ApplicationDbContext _context;

    public GalleryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("gallery")]
    public async Task<IActionResult> Index(int? programmeId = null)
    {
        var query = _context.GalleryImages
            .Include(g => g.Programme)
            .Where(g => g.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (programmeId.HasValue)
        {
            query = query.Where(g => g.ProgrammeId == programmeId.Value);
        }

        var images = await query.OrderBy(g => g.SortOrder).ToListAsync();
        var programmes = await _context.Programmes.AsNoTracking().ToListAsync();

        ViewBag.Programmes = programmes;
        ViewBag.SelectedProgrammeId = programmeId;

        return View(images);
    }
}
