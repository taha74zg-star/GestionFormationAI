using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Lessons.ViewModels;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Lessons.Controllers;

[Authorize(Roles = "Administrator")]
public class LessonsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<LessonsController> _logger;

    public LessonsController(AppDbContext context, ILogger<LessonsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Formation)
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null) return NotFound();

        var lessons = await _context.Lessons
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        ViewBag.ModuleId = moduleId;
        ViewBag.ModuleTitle = module.Title;
        ViewBag.FormationId = module.FormationId;
        ViewBag.FormationTitle = module.Formation.Title;

        return View(lessons);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Formation)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        var model = new LessonDetailViewModel
        {
            Id = lesson.Id,
            ModuleId = lesson.ModuleId,
            FormationId = lesson.Module.FormationId,
            FormationTitle = lesson.Module.Formation.Title,
            ModuleTitle = lesson.Module.Title,
            Title = lesson.Title,
            Content = lesson.Content
        };

        return View(model);
    }

    public async Task<IActionResult> Create(int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Formation)
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module == null) return NotFound();

        ViewBag.ModuleId = moduleId;
        ViewBag.ModuleTitle = module.Title;
        ViewBag.FormationId = module.FormationId;
        ViewBag.FormationTitle = module.Formation.Title;

        var maxOrder = await _context.Lessons
            .Where(l => l.ModuleId == moduleId)
            .MaxAsync(l => (int?)l.SortOrder) ?? 0;

        return View(new LessonFormViewModel
        {
            ModuleId = moduleId,
            SortOrder = maxOrder + 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LessonFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var module = await _context.Modules
                .Include(m => m.Formation)
                .FirstOrDefaultAsync(m => m.Id == model.ModuleId);

            ViewBag.ModuleId = model.ModuleId;
            ViewBag.ModuleTitle = module?.Title ?? "";
            ViewBag.FormationId = module?.FormationId ?? 0;
            ViewBag.FormationTitle = module?.Formation.Title ?? "";
            return View(model);
        }

        var lesson = new Lesson
        {
            ModuleId = model.ModuleId,
            Title = model.Title,
            Content = model.Content,
            SortOrder = model.SortOrder
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Leçon créée: {Title} pour le module {ModuleId}", model.Title, model.ModuleId);
        return RedirectToAction(nameof(Index), new { moduleId = model.ModuleId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Formation)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        ViewBag.ModuleId = lesson.ModuleId;
        ViewBag.ModuleTitle = lesson.Module.Title;
        ViewBag.FormationId = lesson.Module.FormationId;
        ViewBag.FormationTitle = lesson.Module.Formation.Title;

        var model = new LessonFormViewModel
        {
            Id = lesson.Id,
            ModuleId = lesson.ModuleId,
            Title = lesson.Title,
            Content = lesson.Content,
            SortOrder = lesson.SortOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LessonFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();

        lesson.Title = model.Title;
        lesson.Content = model.Content;
        lesson.SortOrder = model.SortOrder;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Leçon mise à jour: {Id}", id);
        return RedirectToAction(nameof(Index), new { moduleId = model.ModuleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int moduleId)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Leçon supprimée: {Id}", id);
        return RedirectToAction(nameof(Index), new { moduleId });
    }
}
