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

    public async Task<IActionResult> OnPostMoveAsync(int formationId, int id, string direction)
    {
        var current = await context.Modules.SingleOrDefaultAsync(m => m.Id == id && m.FormationId == formationId);
        if (current is null) return NotFound();
        var neighbour = direction == "up"
            ? await context.Modules.Where(m => m.FormationId == formationId && m.Ordre < current.Ordre).OrderByDescending(m => m.Ordre).FirstOrDefaultAsync()
            : await context.Modules.Where(m => m.FormationId == formationId && m.Ordre > current.Ordre).OrderBy(m => m.Ordre).FirstOrDefaultAsync();
        if (neighbour is not null) { (current.Ordre, neighbour.Ordre) = (neighbour.Ordre, current.Ordre); await context.SaveChangesAsync(); }
        return RedirectToPage(new { formationId });
    }
}
