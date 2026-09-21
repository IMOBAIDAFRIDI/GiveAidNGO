using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        ApplicationDbContext context,
        IActivityLogger activityLogger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _activityLogger = activityLogger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? role = null)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        ViewBag.SelectedRole = role;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Prevent suspending own admin account
        var currentAdmin = await _userManager.GetUserAsync(User);
        if (currentAdmin?.Id == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot suspend your own administrator account.";
            return RedirectToAction(nameof(Index));
        }

        user.Status = user.Status == UserStatuses.Active ? UserStatuses.Suspended : UserStatuses.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (currentAdmin != null)
        {
            await _activityLogger.LogAsync(currentAdmin.Id, $"Changed user status to {user.Status}", "User", user.Id);
        }

        TempData["SuccessMessage"] = $"User {user.FullName}'s status is now {user.Status}.";
        return RedirectToAction(nameof(Index));
    }
}
