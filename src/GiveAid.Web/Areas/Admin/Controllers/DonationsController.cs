using GiveAid.Web.Data;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class DonationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDonationService _donationService;

    public DonationsController(ApplicationDbContext context, IDonationService donationService)
    {
        _context = context;
        _donationService = donationService;
    }

    public async Task<IActionResult> Index(string? status = null, int? causeId = null)
    {
        var donations = await _donationService.GetAllDonationsAsync(status, causeId);
        var causes = await _context.Causes.AsNoTracking().ToListAsync();

        ViewBag.Causes = causes;
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedCauseId = causeId;

        return View(donations);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var donation = await _context.Donations
            .Include(d => d.Cause)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null) return NotFound();

        return View(donation);
    }
}
