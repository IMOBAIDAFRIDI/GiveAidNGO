using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Programmes;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class ProgrammesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProgrammesController(
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
        var programmes = await _context.Programmes
            .Include(p => p.Interests)
            .OrderByDescending(p => p.StartAt)
            .ToListAsync();
        return View(programmes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProgrammeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgrammeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var slug = model.Title.ToLowerInvariant().Replace(" ", "-").Replace("&", "and");
        if (await _context.Programmes.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        string? imagePath = null;
        if (model.ImageFile != null)
        {
            try
            {
                imagePath = await _mediaService.UploadImageAsync(model.ImageFile, "programmes");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        var programme = new Programme
        {
            Title = model.Title,
            Slug = slug,
            Category = model.Category,
            Description = model.Description,
            StartAt = model.StartAt,
            EndAt = model.EndAt,
            Location = model.Location,
            ImagePath = imagePath,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Programmes.AddAsync(programme);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Created Programme", "Programme", programme.Id);
        }

        TempData["SuccessMessage"] = $"Programme '{programme.Title}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var programme = await _context.Programmes.FindAsync(id);
        if (programme == null) return NotFound();

        var model = new ProgrammeEditViewModel
        {
            Id = programme.Id,
            Title = programme.Title,
            Category = programme.Category ?? "Education",
            Description = programme.Description,
            StartAt = programme.StartAt,
            EndAt = programme.EndAt,
            Location = programme.Location,
            ExistingImagePath = programme.ImagePath,
            Status = programme.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgrammeEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var programme = await _context.Programmes.FindAsync(id);
        if (programme == null) return NotFound();

        if (model.ImageFile != null)
        {
            try
            {
                var newPath = await _mediaService.UploadImageAsync(model.ImageFile, "programmes");
                if (newPath != null)
                {
                    _mediaService.DeleteImage(programme.ImagePath);
                    programme.ImagePath = newPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
                return View(model);
            }
        }

        programme.Title = model.Title;
        programme.Category = model.Category;
        programme.Description = model.Description;
        programme.StartAt = model.StartAt;
        programme.EndAt = model.EndAt;
        programme.Location = model.Location;
        programme.Status = model.Status;
        programme.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated Programme", "Programme", programme.Id);
        }

        TempData["SuccessMessage"] = $"Programme '{programme.Title}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var programme = await _context.Programmes.FindAsync(id);
        if (programme == null) return NotFound();

        programme.DeletedAt = DateTime.UtcNow;
        programme.Status = ProgrammeStatuses.Closed;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Soft-Deleted Programme", "Programme", programme.Id);
        }

        TempData["SuccessMessage"] = $"Programme '{programme.Title}' has been soft-deleted.";
        return RedirectToAction(nameof(Index));
    }
}
