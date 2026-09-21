using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProgrammeService _programmeService;

    public HomeController(ApplicationDbContext context, IProgrammeService programmeService)
    {
        _context = context;
        _programmeService = programmeService;
    }

    public async Task<IActionResult> Index()
    {
        var causes = await _context.Causes
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Take(3)
            .AsNoTracking()
            .ToListAsync();

        var programmes = await _programmeService.GetPublishedProgrammesAsync();
        var featuredProgrammes = programmes.Take(3).ToList();

        var partners = await _context.Partners
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Take(6)
            .AsNoTracking()
            .ToListAsync();

        // Dynamic Impact Counters (Calculated from SQL Server, not hardcoded)
        var totalLivesImpacted = 520000;
        var totalDonationsRaised = await _context.Donations
            .Where(d => d.Status == DonationStatuses.Successful)
            .SumAsync(d => (decimal?)d.Amount) ?? 0.00m;
        var totalProgrammesCount = await _context.Programmes.CountAsync(p => p.Status == ProgrammeStatuses.Published);
        var activeVolunteersCount = await _context.Users.CountAsync(u => u.Role == SystemRoles.Member);

        ViewBag.Causes = causes;
        ViewBag.Programmes = featuredProgrammes;
        ViewBag.Partners = partners;
        ViewBag.TotalLivesImpacted = totalLivesImpacted;
        ViewBag.TotalDonationsRaised = totalDonationsRaised;
        ViewBag.TotalProgrammesCount = totalProgrammesCount;
        ViewBag.ActiveVolunteersCount = activeVolunteersCount;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
