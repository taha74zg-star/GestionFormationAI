using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

[Authorize(Roles = "Admin")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Formation Formation { get; private set; } = default!;
    public IList<AIFormationPlatform.Core.Entities.Module> Modules { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int formationId)
    {
        var formation = await context.Formations.FindAsync(formationId);
        if (formation is null) return NotFound();
        Formation = formation;
        Modules = await context.Modules.Where(m => m.FormationId == formationId).OrderBy(m => m.Ordre).ToListAsync();
        return Page();
    }
}
