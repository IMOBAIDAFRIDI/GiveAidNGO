using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.About;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class AboutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AboutController(
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
        var sections = await _context.AboutSections
            .OrderBy(a => a.SortOrder)
            .ToListAsync();
        return View(sections);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AboutSectionCreateViewModel
        {
            SortOrder = (_context.AboutSections.Max(a => (int?)a.SortOrder) ?? 0) + 1,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AboutSectionCreateViewModel model)
    {
        model.Slug = model.Slug.Trim().ToLowerInvariant();

        if (await _context.AboutSections.AnyAsync(a => a.Slug.ToLower() == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already in use. Please choose a unique slug.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? imagePath = null;
        if (model.ImageFile != null)
        {
            try
            {
                imagePath = await _mediaService.UploadImageAsync(model.ImageFile, "about");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        var admin = await _userManager.GetUserAsync(User);

        var section = new AboutSection
        {
            Title = model.Title.Trim(),
            Slug = model.Slug,
            Content = model.Content.Trim(),
            ImagePath = imagePath,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = admin?.Id
        };

        await _context.AboutSections.AddAsync(section);
        await _context.SaveChangesAsync();

        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Created About Section", "AboutSection", section.Id);
        }

        TempData["SuccessMessage"] = $"About Section '{section.Title}' created successfully.";
        return RedirectToAction(nameof(Index), "About", new { area = "Admin" });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var section = await _context.AboutSections.FindAsync(id);
        if (section == null) return NotFound();

        var model = new AboutSectionEditViewModel
        {
            Id = section.Id,
            Slug = section.Slug,
            Title = section.Title,
            Content = section.Content,
            SortOrder = section.SortOrder,
            IsActive = section.IsActive,
            ExistingImagePath = section.ImagePath
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AboutSectionEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        model.Slug = model.Slug.Trim().ToLowerInvariant();

        if (await _context.AboutSections.AnyAsync(a => a.Slug.ToLower() == model.Slug && a.Id != id))
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already in use by another section.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var section = await _context.AboutSections.FindAsync(id);
        if (section == null) return NotFound();

        if (model.ImageFile != null)
        {
            try
            {
                var newPath = await _mediaService.UploadImageAsync(model.ImageFile, "about");
                if (newPath != null)
                {
                    _mediaService.DeleteImage(section.ImagePath);
                    section.ImagePath = newPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        var admin = await _userManager.GetUserAsync(User);

        section.Title = model.Title.Trim();
        section.Slug = model.Slug;
        section.Content = model.Content.Trim();
        section.SortOrder = model.SortOrder;
        section.IsActive = model.IsActive;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedByUserId = admin?.Id;

        await _context.SaveChangesAsync();

        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated About Section", "AboutSection", section.Id);
        }

        TempData["SuccessMessage"] = $"About Section '{section.Title}' updated successfully.";
        return RedirectToAction(nameof(Index), "About", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _context.AboutSections.FindAsync(id);
        if (section == null) return NotFound();

        if (!string.IsNullOrEmpty(section.ImagePath))
        {
            _mediaService.DeleteImage(section.ImagePath);
        }

        _context.AboutSections.Remove(section);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Deleted About Section", "AboutSection", id);
        }

        TempData["SuccessMessage"] = $"About Section '{section.Title}' deleted successfully.";
        return RedirectToAction(nameof(Index), "About", new { area = "Admin" });
    }
}
