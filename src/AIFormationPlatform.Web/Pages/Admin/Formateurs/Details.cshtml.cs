using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public FormateurEntity Formateur { get; private set; } = default!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var formateur = await context.Formateurs.FindAsync(id);
        if (formateur is null) return NotFound();
        Formateur = formateur;
        return Page();
    }
}
