using GiveAid.Web.Data;
using GiveAid.Web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class ActivityLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ActivityLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _context.AdminActivityLogs
            .Include(al => al.AdminUser)
            .OrderByDescending(al => al.CreatedAt)
            .Take(100)
            .AsNoTracking()
            .ToListAsync();
        return View(logs);
    }
}
