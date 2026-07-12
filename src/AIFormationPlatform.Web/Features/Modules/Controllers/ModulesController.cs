using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Modules.ViewModels;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Modules.Controllers;

[Authorize(Roles = "Administrator")]
public class ModulesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ModulesController> _logger;

    public ModulesController(AppDbContext context, ILogger<ModulesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int formationId)
    {
        var formation = await _context.Formations.FindAsync(formationId);
        if (formation == null) return NotFound();

        var modules = await _context.Modules
            .Where(m => m.FormationId == formationId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        ViewBag.FormationId = formationId;
        ViewBag.FormationTitle = formation.Title;

        return View(modules);
    }

    public async Task<IActionResult> Create(int formationId)
    {
        var formation = await _context.Formations.FindAsync(formationId);
        if (formation == null) return NotFound();

        ViewBag.FormationId = formationId;
        ViewBag.FormationTitle = formation.Title;

        var maxOrder = await _context.Modules
            .Where(m => m.FormationId == formationId)
            .MaxAsync(m => (int?)m.SortOrder) ?? 0;

        return View(new ModuleFormViewModel
        {
            FormationId = formationId,
            SortOrder = maxOrder + 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ModuleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var formation = await _context.Formations.FindAsync(model.FormationId);
            ViewBag.FormationId = model.FormationId;
            ViewBag.FormationTitle = formation?.Title ?? "";
            return View(model);
        }

        var module = new Module
        {
            FormationId = model.FormationId,
            Title = model.Title,
            Description = model.Description,
            SortOrder = model.SortOrder
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Module créé: {Title} pour la formation {FormationId}", model.Title, model.FormationId);
        return RedirectToAction(nameof(Index), new { formationId = model.FormationId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null) return NotFound();

        var formation = await _context.Formations.FindAsync(module.FormationId);
        ViewBag.FormationId = module.FormationId;
        ViewBag.FormationTitle = formation?.Title ?? "";

        var model = new ModuleFormViewModel
        {
            Id = module.Id,
            FormationId = module.FormationId,
            Title = module.Title,
            Description = module.Description,
            SortOrder = module.SortOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ModuleFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var module = await _context.Modules.FindAsync(id);
        if (module == null) return NotFound();

        module.Title = model.Title;
        module.Description = model.Description;
        module.SortOrder = model.SortOrder;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Module mis à jour: {Id}", id);
        return RedirectToAction(nameof(Index), new { formationId = model.FormationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int formationId)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null) return NotFound();

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Module supprimé: {Id}", id);
        return RedirectToAction(nameof(Index), new { formationId });
    }
}
