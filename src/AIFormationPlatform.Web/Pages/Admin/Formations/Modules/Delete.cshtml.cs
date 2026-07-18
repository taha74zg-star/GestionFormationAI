using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

[Authorize(Roles = "Admin")]
public class DeleteModel(ApplicationDbContext context) : PageModel
{
    public AIFormationPlatform.Core.Entities.Module Module { get; private set; } = default!;
    public async Task<IActionResult> OnGetAsync(int formationId, int id)
    {
        var module = await context.Modules.FindAsync(id);
        if (module is null || module.FormationId != formationId) return NotFound();
        Module = module;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int formationId, int id)
    {
        var module = await context.Modules.FindAsync(id);
        if (module is null || module.FormationId != formationId) return NotFound();
        context.Modules.Remove(module);
        await context.SaveChangesAsync();
        return RedirectToPage("Index", new { formationId });
    }
}
