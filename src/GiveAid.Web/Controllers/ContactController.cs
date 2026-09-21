using GiveAid.Web.Data;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiveAid.Web.Models.Entities;

namespace GiveAid.Web.Controllers;

public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IQueryService _queryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactController(
        ApplicationDbContext context,
        IQueryService queryService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _queryService = queryService;
        _userManager = userManager;
    }

    [HttpGet("contact")]
    public async Task<IActionResult> Index()
    {
        var primaryContact = await _context.ContactInfos
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefaultAsync();

        ViewBag.ContactInfo = primaryContact;
        return View(new QueryCreateViewModel());
    }

    [HttpPost("contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(QueryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ContactInfo = await _context.ContactInfos.FirstOrDefaultAsync(c => c.IsActive && c.IsPrimary);
            return View(model);
        }

        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        await _queryService.SubmitQueryAsync(model, userId);
        TempData["SuccessMessage"] = "Your message has been sent successfully. Our team will get back to you shortly!";
        return RedirectToAction(nameof(Index));
    }
}
