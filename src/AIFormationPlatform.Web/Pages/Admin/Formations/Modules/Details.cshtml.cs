using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

[Authorize(Roles = "Admin")]
public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public AIFormationPlatform.Core.Entities.Module Module { get; private set; } = default!;
    public async Task<IActionResult> OnGetAsync(int formationId, int id)
    {
        var module = await context.Modules.Include(m => m.Exercice).Include(m => m.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Reponses)
            .SingleOrDefaultAsync(m => m.Id == id && m.FormationId == formationId);
        if (module is null) return NotFound();
        Module = module;
        return Page();
    }
}
