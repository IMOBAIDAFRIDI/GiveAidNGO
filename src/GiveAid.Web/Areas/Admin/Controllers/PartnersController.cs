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
public class PartnersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public PartnersController(
        ApplicationDbContext context,
        IMediaService mediaService,
        IActivityLogger activityLogger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _mediaService = mediaService;
        _activityLogger = activityLogger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var partners = await _context.Partners
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
        return View(partners);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Partner { SortOrder = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Partner model, IFormFile? logoFile)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = model.Name.ToLowerInvariant().Replace(" ", "-");
        }

        if (logoFile != null)
        {
            try
            {
                model.LogoPath = await _mediaService.UploadImageAsync(logoFile, "partners");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("logoFile", ex.Message);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        await _context.Partners.AddAsync(model);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Created Partner", "Partner", model.Id);
        }

        TempData["SuccessMessage"] = $"Partner '{model.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner == null) return NotFound();
        return View(partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Partner model, IFormFile? logoFile)
    {
        if (id != model.Id) return BadRequest();

        var partner = await _context.Partners.FindAsync(id);
        if (partner == null) return NotFound();

        if (logoFile != null)
        {
            try
            {
                var newPath = await _mediaService.UploadImageAsync(logoFile, "partners");
                if (newPath != null)
                {
                    _mediaService.DeleteImage(partner.LogoPath);
                    partner.LogoPath = newPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("logoFile", ex.Message);
                return View(model);
            }
        }

        partner.Name = model.Name;
        partner.Slug = model.Slug;
        partner.Description = model.Description;
        partner.Website = model.Website;
        partner.IsActive = model.IsActive;
        partner.SortOrder = model.SortOrder;
        partner.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated Partner", "Partner", partner.Id);
        }

        TempData["SuccessMessage"] = $"Partner '{partner.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner == null) return NotFound();

        partner.DeletedAt = DateTime.UtcNow;
        partner.IsActive = false;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Soft-Deleted Partner", "Partner", partner.Id);
        }

        TempData["SuccessMessage"] = $"Partner '{partner.Name}' removed.";
        return RedirectToAction(nameof(Index));
    }
}
