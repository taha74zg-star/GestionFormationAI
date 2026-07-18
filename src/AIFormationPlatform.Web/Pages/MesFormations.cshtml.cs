using System.Security.Claims;
using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages;

[Authorize(Roles = "Apprenant")]
public class MesFormationsModel(ApplicationDbContext context) : PageModel
{
    public IList<Inscription> Inscriptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var apprenantId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Inscriptions = await context.Inscriptions
            .Where(i => i.ApprenantId == apprenantId)
            .Include(i => i.Formation)
            .OrderByDescending(i => i.DateInscription)
            .ToListAsync();
    }
}
