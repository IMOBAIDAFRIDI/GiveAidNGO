using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.ViewModels.Account;
using GiveAid.Web.Models.ViewModels.Queries;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GiveAid.Web.Controllers;

[Authorize]
[Route("member")]
public class MemberController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDonationService _donationService;
    private readonly IProgrammeService _programmeService;
    private readonly IQueryService _queryService;
    private readonly IInvitationService _invitationService;

    public MemberController(
        UserManager<ApplicationUser> userManager,
        IDonationService donationService,
        IProgrammeService programmeService,
        IQueryService queryService,
        IInvitationService invitationService)
    {
        _userManager = userManager;
        _donationService = donationService;
        _programmeService = programmeService;
        _queryService = queryService;
        _invitationService = invitationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var donations = await _donationService.GetUserDonationsAsync(user.Id);
        var interests = await _programmeService.GetUserInterestsAsync(user.Id);
        var queries = await _queryService.GetUserQueriesAsync(user.Id);
        var invitations = await _invitationService.GetUserInvitationsAsync(user.Id);

        ViewBag.User = user;
        ViewBag.Donations = donations;
        ViewBag.Interests = interests;
        ViewBag.Queries = queries;
        ViewBag.Invitations = invitations;

        return View();
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            City = user.City,
            Profession = user.Profession,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            MemberSince = user.CreatedAt
        };

        return View(model);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        user.City = model.City;
        user.Profession = model.Profession;
        user.Gender = model.Gender;
        user.DateOfBirth = model.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost("programmes/{id:int}/interest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterInterest(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _programmeService.RegisterInterestAsync(id, user.Id);
        
        // Return JSON if AJAX request, otherwise redirect with TempData
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, alreadyRegistered = result.AlreadyRegistered, message = result.Message });
        }

        if (result.AlreadyRegistered)
        {
            TempData["InfoMessage"] = result.Message;
        }
        else if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction("Details", "Programme", new { id });
    }

    [HttpGet("queries")]
    public async Task<IActionResult> Queries()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var queries = await _queryService.GetUserQueriesAsync(user.Id);
        return View(queries);
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> Invitations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var invitations = await _invitationService.GetUserInvitationsAsync(user.Id);
        ViewBag.Invitations = invitations;
        return View(new InvitationCreateViewModel());
    }

    [HttpPost("invitations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvitation(InvitationCreateViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            ViewBag.Invitations = await _invitationService.GetUserInvitationsAsync(user.Id);
            return View("Invitations", model);
        }

        await _invitationService.SendInvitationAsync(model, user.Id);
        TempData["SuccessMessage"] = $"An invitation token has been generated and sent to {model.RecipientEmail}!";
        return RedirectToAction(nameof(Invitations));
    }
}
