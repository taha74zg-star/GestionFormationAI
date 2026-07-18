using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<FormateurEntity> Formateurs { get; private set; } = [];
    public async Task OnGetAsync() => Formateurs = await context.Formateurs.OrderBy(f => f.Nom).ThenBy(f => f.Prenom).ToListAsync();
}
