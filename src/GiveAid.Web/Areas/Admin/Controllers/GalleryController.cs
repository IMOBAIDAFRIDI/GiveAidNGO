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
public class GalleryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public GalleryController(
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
        var images = await _context.GalleryImages
            .Include(g => g.Programme)
            .Include(g => g.UploadedByUser)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
        return View(images);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Programmes = await _context.Programmes.AsNoTracking().ToListAsync();
        return View(new GalleryImage());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GalleryImage model, IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError("imageFile", "Please select an image file to upload.");
        }
        else
        {
            try
            {
                var uploadedPath = await _mediaService.UploadImageAsync(imageFile, "gallery");
                if (uploadedPath != null)
                {
                    model.ImagePath = uploadedPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
        }

        var admin = await _userManager.GetUserAsync(User);
        if (admin == null) return Challenge();

        model.UploadedByUserId = admin.Id;
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        ModelState.Remove(nameof(model.UploadedByUser));
        ModelState.Remove(nameof(model.Programme));

        if (!ModelState.IsValid)
        {
            ViewBag.Programmes = await _context.Programmes.AsNoTracking().ToListAsync();
            return View(model);
        }

        await _context.GalleryImages.AddAsync(model);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(admin.Id, "Uploaded Gallery Image", "GalleryImage", model.Id);

        TempData["SuccessMessage"] = "Image uploaded to gallery successfully.";
        return RedirectToAction(nameof(Index), "Gallery", new { area = "Admin" });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var image = await _context.GalleryImages.FindAsync(id);
        if (image == null) return NotFound();

        ViewBag.Programmes = await _context.Programmes.AsNoTracking().ToListAsync();
        return View(image);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GalleryImage model, IFormFile? imageFile)
    {
        if (id != model.Id) return BadRequest();

        var image = await _context.GalleryImages.FindAsync(id);
        if (image == null) return NotFound();

        if (imageFile != null && imageFile.Length > 0)
        {
            try
            {
                var uploadedPath = await _mediaService.UploadImageAsync(imageFile, "gallery");
                if (uploadedPath != null)
                {
                    _mediaService.DeleteImage(image.ImagePath);
                    image.ImagePath = uploadedPath;
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
        }

        ModelState.Remove(nameof(model.UploadedByUser));
        ModelState.Remove(nameof(model.Programme));
        ModelState.Remove(nameof(model.ImagePath));

        if (!ModelState.IsValid)
        {
            ViewBag.Programmes = await _context.Programmes.AsNoTracking().ToListAsync();
            return View(model);
        }

        image.Title = model.Title;
        image.Caption = model.Caption;
        image.ProgrammeId = model.ProgrammeId;
        image.SortOrder = model.SortOrder;
        image.IsActive = model.IsActive;
        image.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Updated Gallery Image", "GalleryImage", image.Id);
        }

        TempData["SuccessMessage"] = "Gallery image updated successfully.";
        return RedirectToAction(nameof(Index), "Gallery", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _context.GalleryImages.FindAsync(id);
        if (image == null) return NotFound();

        _mediaService.DeleteImage(image.ImagePath);
        _context.GalleryImages.Remove(image);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Deleted Gallery Image", "GalleryImage", id);
        }

        TempData["SuccessMessage"] = "Gallery image deleted.";
        return RedirectToAction(nameof(Index), "Gallery", new { area = "Admin" });
    }
}
