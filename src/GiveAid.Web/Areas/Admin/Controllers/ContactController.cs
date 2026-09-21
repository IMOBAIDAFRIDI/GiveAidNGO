using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemRoles.Admin)]
public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var contact = await _context.ContactInfos.FirstOrDefaultAsync(c => c.IsPrimary) 
                      ?? await _context.ContactInfos.FirstOrDefaultAsync()
                      ?? new ContactInfo();
        return View(contact);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactInfo model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var contact = await _context.ContactInfos.FindAsync(model.Id);
        if (contact == null)
        {
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            await _context.ContactInfos.AddAsync(model);
        }
        else
        {
            contact.Label = model.Label;
            contact.Address = model.Address;
            contact.Phone = model.Phone;
            contact.Email = model.Email;
            contact.MapUrl = model.MapUrl;
            contact.SocialLinksJson = model.SocialLinksJson;
            contact.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Contact information updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
