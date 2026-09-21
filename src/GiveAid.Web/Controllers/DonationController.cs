using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Donations;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

[Route("donate")]
public class DonationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDonationService _donationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DonationController> _logger;

    public DonationController(
        ApplicationDbContext context,
        IDonationService donationService,
        UserManager<ApplicationUser> userManager,
        ILogger<DonationController> logger)
    {
        _context = context;
        _donationService = donationService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? causeId = null, decimal? amount = null)
    {
        var model = new DonationCreateViewModel();

        if (amount.HasValue && amount.Value > 0)
        {
            model.Amount = amount.Value;
        }

        if (causeId.HasValue)
        {
            model.CauseId = causeId.Value;
        }

        // Populate donor info if logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.DonorName = user.FullName;
                model.DonorEmail = user.Email ?? string.Empty;
            }
        }

        await PopulateCausesSelectList(model);
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DonationCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCausesSelectList(model);
            return View(model);
        }

        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        try
        {
            var result = await _donationService.ProcessDemoDonationAsync(model, userId);
            TempData["SuccessMessage"] = "Thank you for your generous contribution! Your payment has been authorized.";
            return RedirectToAction(nameof(Result), new { reference = result.ReferenceNo });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCausesSelectList(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing donation for donor {DonorEmail}", model.DonorEmail);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while processing your donation. Please try again.");
            await PopulateCausesSelectList(model);
            return View(model);
        }
    }

    [HttpGet("result")]
    public async Task<IActionResult> Result(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = await _donationService.GetDonationByReferenceAsync(reference);
        if (result == null)
        {
            return NotFound();
        }

        return View(result);
    }

    private async Task PopulateCausesSelectList(DonationCreateViewModel model)
    {
        var causes = await _context.Causes
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        model.AvailableCauses = causes.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.Name} (Goal: ${c.TargetAmount:N0})"
        }).ToList();
    }
}
