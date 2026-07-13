using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIFormationPlatform.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin,Formateur")]
public class FormationController : Controller
{
    private readonly IFormationService _formationService;

    public FormationController(IFormationService formationService)
    {
        _formationService = formationService;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _formationService.GetAllAsync();
        return View(list);
    }

    public IActionResult Create()
    {
        return View(new Formation());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Formation model)
    {
        if (!ModelState.IsValid) return View(model);
        await _formationService.CreateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var f = await _formationService.GetByIdAsync(id);
        if (f == null) return NotFound();
        return View(f);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Formation model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        model.DateModification = DateTime.UtcNow;
        await _formationService.UpdateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _formationService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var f = await _formationService.GetByIdAsync(id);
        if (f == null) return NotFound();
        return View(f);
    }
}
