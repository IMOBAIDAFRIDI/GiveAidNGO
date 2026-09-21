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
public class NGOsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public NGOsController(
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
        var ngos = await _context.NGOs
            .OrderBy(n => n.Name)
            .ToListAsync();
        return View(ngos);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NGO());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NGO model, IFormFile? logoFile)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = model.Name.ToLowerInvariant().Replace(" ", "-");
        }

        if (logoFile != null)
        {
            try
            {
                model.LogoPath = await _mediaService.UploadImageAsync(logoFile, "ngos");
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

        await _context.NGOs.AddAsync(model);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Created NGO", "NGO", model.Id);
        }

        TempData["SuccessMessage"] = $"NGO '{model.Name}' added to directory.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ngo = await _context.NGOs.FindAsync(id);
        if (ngo == null) return NotFound();
        return View(ngo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NGO model, IFormFile? logoFile)
    {
        if (id != model.Id) return BadRequest();

        var ngo = await _context.NGOs.FindAsync(id);
        if (ngo == null) return NotFound();

        if (logoFile != null)
        {
            try
            {
                var newPath = await _mediaService.UploadImageAsync(logoFile, "ngos");
                if (newPath != null)
                {
                    _mediaService.DeleteImage(ngo.LogoPath);
                    ngo.LogoPath = newPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("logoFile", ex.Message);
                return View(model);
            }
        }

        ngo.Name = model.Name;
        ngo.Slug = model.Slug;
        ngo.Description = model.Description;
        ngo.City = model.City;
        ngo.Address = model.Address;
        ngo.Phone = model.Phone;
        ngo.Email = model.Email;
        ngo.Website = model.Website;
        ngo.IsActive = model.IsActive;
        ngo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated NGO", "NGO", ngo.Id);
        }

        TempData["SuccessMessage"] = $"NGO '{ngo.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ngo = await _context.NGOs.FindAsync(id);
        if (ngo == null) return NotFound();

        ngo.DeletedAt = DateTime.UtcNow;
        ngo.IsActive = false;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Soft-Deleted NGO", "NGO", ngo.Id);
        }

        TempData["SuccessMessage"] = $"NGO '{ngo.Name}' removed from active directory.";
        return RedirectToAction(nameof(Index));
    }
}
