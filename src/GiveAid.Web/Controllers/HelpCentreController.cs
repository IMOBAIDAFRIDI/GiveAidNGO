using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GiveAid.Web.Controllers;

[Route("help")]
[Route("help-centre")]
[Route("helpcentre")]
public class HelpCentreController : Controller
{
    private readonly IQueryService _queryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HelpCentreController(
        IQueryService queryService,
        UserManager<ApplicationUser> userManager)
    {
        _queryService = queryService;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = new QueryCreateViewModel();

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.Name = user.FullName;
                model.Email = user.Email ?? string.Empty;
            }
        }

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(QueryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        var query = await _queryService.SubmitQueryAsync(model, userId);
        TempData["SuccessMessage"] = $"Your ticket #{query.Id} has been created. A support officer will review your message.";
        return RedirectToAction(nameof(Index));
    }
}
