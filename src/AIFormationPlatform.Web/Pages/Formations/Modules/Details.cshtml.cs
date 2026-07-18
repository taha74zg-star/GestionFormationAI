using System.Security.Claims;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Formations.Modules;

[Authorize(Roles = "Apprenant")]
public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public AIFormationPlatform.Core.Entities.Module Module { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(int formationId, int id)
    {
        var apprenantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var estInscrit = await context.Inscriptions.AnyAsync(
            i => i.ApprenantId == apprenantId && i.FormationId == formationId);
        if (!estInscrit) return Forbid();

        var module = await context.Modules.SingleOrDefaultAsync(m => m.Id == id && m.FormationId == formationId);
        if (module is null) return NotFound();
        Module = module;
        return Page();
    }
}
