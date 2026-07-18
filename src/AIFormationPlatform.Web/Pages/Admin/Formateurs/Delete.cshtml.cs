using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class DeleteModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public FormateurEntity Formateur { get; set; } = default!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var formateur = await context.Formateurs.FindAsync(id);
        if (formateur is null) return NotFound();
        Formateur = formateur;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var formateur = await context.Formateurs.FindAsync(Formateur.Id);
        if (formateur is null) return NotFound();
        context.Formateurs.Remove(formateur);
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
