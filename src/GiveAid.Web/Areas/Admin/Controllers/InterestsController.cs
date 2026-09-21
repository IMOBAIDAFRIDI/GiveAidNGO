using GiveAid.Web.Data;
using GiveAid.Web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class InterestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public InterestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? programmeId = null)
    {
        var query = _context.ProgrammeInterests
            .Include(pi => pi.Programme)
            .Include(pi => pi.User)
            .AsNoTracking()
            .AsQueryable();

        if (programmeId.HasValue)
        {
            query = query.Where(pi => pi.ProgrammeId == programmeId.Value);
        }

        var interests = await query.OrderByDescending(pi => pi.InterestedAt).ToListAsync();
        var programmes = await _context.Programmes.AsNoTracking().ToListAsync();

        ViewBag.Programmes = programmes;
        ViewBag.SelectedProgrammeId = programmeId;

        return View(interests);
    }
}
