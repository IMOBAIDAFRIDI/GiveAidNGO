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

        section.Title = model.Title;
        section.Content = model.Content;
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
        return RedirectToAction(nameof(Index));
    }
}
