using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Shared.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var publishedFormations = await _context.Formations
            .Where(f => f.IsPublished)
            .Select(f => new FormationCatalogItem
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                ModuleCount = f.Modules.Count
            })
            .ToListAsync();

        return View(publishedFormations);
    }
}

public class FormationCatalogItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
}
