using System.Security.Claims;
using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Formations;

[Authorize(Roles = "Apprenant")]
public class InscriptionModel(ApplicationDbContext context) : PageModel
{
    public Formation Formation { get; private set; } = default!;
    public bool DejaInscrit { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var formation = await context.Formations.FindAsync(id);
        if (formation is null) return NotFound();
        Formation = formation;
        DejaInscrit = await context.Inscriptions.AnyAsync(i => i.FormationId == id && i.ApprenantId == GetApprenantId());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await context.Formations.AnyAsync(f => f.Id == id)) return NotFound();
        var apprenantId = GetApprenantId();
        if (await context.Inscriptions.AnyAsync(i => i.FormationId == id && i.ApprenantId == apprenantId))
            return RedirectToPage("/MesFormations");

        context.Inscriptions.Add(new Inscription { ApprenantId = apprenantId, FormationId = id });
        await context.SaveChangesAsync();
        return RedirectToPage("/MesFormations");
    }

    private string GetApprenantId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
