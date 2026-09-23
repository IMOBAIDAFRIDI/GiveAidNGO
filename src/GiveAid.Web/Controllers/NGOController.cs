using GiveAid.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

public class NGOController : Controller
{
    private readonly ApplicationDbContext _context;

    public NGOController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("ngos")]
    [HttpGet("ngo")]
    public async Task<IActionResult> Index(string? city = null, string? search = null)
    {
        var query = _context.NGOs
            .Where(n => n.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(n => n.City == city);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.Name.Contains(search) || n.Description.Contains(search));
        }

        var ngos = await query.OrderBy(n => n.Name).ToListAsync();

        var cities = await _context.NGOs
            .Where(n => n.IsActive && n.City != null)
            .Select(n => n.City!)
            .Distinct()
            .ToListAsync();

        ViewBag.Cities = cities;
        ViewBag.SelectedCity = city;
        ViewBag.SearchTerm = search;

        return View(ngos);
    }
}
