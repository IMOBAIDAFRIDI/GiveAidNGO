using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class QueriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IQueryService _queryService;
    private readonly IActivityLogger _activityLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public QueriesController(
        ApplicationDbContext context,
        IQueryService queryService,
        IActivityLogger activityLogger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _queryService = queryService;
        _activityLogger = activityLogger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var queries = await _queryService.GetAllQueriesAsync(status);
        ViewBag.SelectedStatus = status;
        return View(queries);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var query = await _queryService.GetQueryByIdAsync(id);
        if (query == null) return NotFound();

        var replyModel = new QueryReplyViewModel
        {
            QueryId = query.Id,
            Query = query
        };

        return View(replyModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(QueryReplyViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ReplyMessage))
        {
            ModelState.AddModelError(nameof(model.ReplyMessage), "Please enter a reply message.");
            model.Query = (await _queryService.GetQueryByIdAsync(model.QueryId))!;
            return View("Details", model);
        }

        var admin = await _userManager.GetUserAsync(User);
        if (admin == null) return Challenge();

        var success = await _queryService.ReplyToQueryAsync(model.QueryId, admin.Id, model.ReplyMessage);
        if (success)
        {
            await _activityLogger.LogAsync(admin.Id, "Replied to Query", "Query", model.QueryId);
            TempData["SuccessMessage"] = $"Reply recorded for Ticket #{model.QueryId}.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not record reply.";
        }

        return RedirectToAction(nameof(Details), new { id = model.QueryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseQuery(int id)
    {
        var query = await _context.Queries.FindAsync(id);
        if (query == null) return NotFound();

        query.Status = QueryStatuses.Closed;
        query.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        if (admin != null)
        {
            await _activityLogger.LogAsync(admin.Id, "Closed Query", "Query", id);
        }

        TempData["SuccessMessage"] = $"Ticket #{id} has been marked as Closed.";
        return RedirectToAction(nameof(Index));
    }
}
