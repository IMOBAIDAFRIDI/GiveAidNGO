using GiveAid.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

public class PartnerController : Controller
{
    private readonly ApplicationDbContext _context;

    public PartnerController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("partners")]
    [HttpGet("partner")]
    public async Task<IActionResult> Index()
    {
        var partners = await _context.Partners
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return View(partners);
    }
}
