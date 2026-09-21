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
public class CausesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public CausesController(
        ApplicationDbContext context,
        IActivityLogger activityLogger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _activityLogger = activityLogger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var causes = await _context.Causes
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(causes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Cause { TargetAmount = 10000 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cause model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = model.Name.ToLowerInvariant().Replace(" ", "-").Replace("&", "and");
        }

        if (await _context.Causes.AnyAsync(c => c.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "A cause with this slug already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        await _context.Causes.AddAsync(model);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Created Cause", "Cause", model.Id);
        }

        TempData["SuccessMessage"] = $"Cause '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cause = await _context.Causes.FindAsync(id);
        if (cause == null) return NotFound();
        return View(cause);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cause model)
    {
        if (id != model.Id) return BadRequest();

        if (await _context.Causes.AnyAsync(c => c.Slug == model.Slug && c.Id != id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Slug is already in use by another cause.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cause = await _context.Causes.FindAsync(id);
        if (cause == null) return NotFound();

        cause.Name = model.Name;
        cause.Slug = model.Slug;
        cause.Description = model.Description;
        cause.TargetAmount = model.TargetAmount;
        cause.IsActive = model.IsActive;
        cause.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated Cause", "Cause", cause.Id);
        }

        TempData["SuccessMessage"] = $"Cause '{cause.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var cause = await _context.Causes.FindAsync(id);
        if (cause == null) return NotFound();

        cause.IsActive = !cause.IsActive;
        cause.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cause '{cause.Name}' status set to {(cause.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cause = await _context.Causes.FindAsync(id);
        if (cause == null) return NotFound();

        // Soft delete
        cause.DeletedAt = DateTime.UtcNow;
        cause.IsActive = false;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Soft-Deleted Cause", "Cause", cause.Id);
        }

        TempData["SuccessMessage"] = $"Cause '{cause.Name}' has been removed.";
        return RedirectToAction(nameof(Index));
    }
}
