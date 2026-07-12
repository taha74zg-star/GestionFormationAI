using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Formations.ViewModels;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Formations.Controllers;

[Authorize(Roles = "Administrator")]
public class FormationsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormationsController> _logger;

    public FormationsController(AppDbContext context, ILogger<FormationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var formations = await _context.Formations
            .Select(f => new FormationListViewModel
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                IsPublished = f.IsPublished,
                ModuleCount = f.Modules.Count,
                EnrollmentCount = f.Enrollments.Count,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return View(formations);
    }

    public async Task<IActionResult> Details(int id)
    {
        var formation = await _context.Formations
            .Include(f => f.Modules.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formation == null) return NotFound();

        var viewModel = new FormationDetailViewModel
        {
            Id = formation.Id,
            Title = formation.Title,
            Description = formation.Description,
            IsPublished = formation.IsPublished,
            CreatedAt = formation.CreatedAt,
            Modules = formation.Modules.Select(m => new ModuleSummaryViewModel
            {
                Id = m.Id,
                Title = m.Title,
                SortOrder = m.SortOrder,
                LessonCount = m.Lessons.Count
            }).ToList()
        };

        return View(viewModel);
    }

    public IActionResult Create() => View(new FormationFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FormationFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var formation = new Formation
        {
            Title = model.Title,
            Description = model.Description,
            IsPublished = model.IsPublished,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Formations.Add(formation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Formation créée: {Title}", model.Title);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var formation = await _context.Formations.FindAsync(id);
        if (formation == null) return NotFound();

        var model = new FormationFormViewModel
        {
            Id = formation.Id,
            Title = formation.Title,
            Description = formation.Description,
            IsPublished = formation.IsPublished
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FormationFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var formation = await _context.Formations.FindAsync(id);
        if (formation == null) return NotFound();

        formation.Title = model.Title;
        formation.Description = model.Description;
        formation.IsPublished = model.IsPublished;
        formation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Formation mise à jour: {Id}", id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var formation = await _context.Formations.FindAsync(id);
        if (formation == null) return NotFound();

        _context.Formations.Remove(formation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Formation supprimée: {Id}", id);
        return RedirectToAction(nameof(Index));
    }
}
