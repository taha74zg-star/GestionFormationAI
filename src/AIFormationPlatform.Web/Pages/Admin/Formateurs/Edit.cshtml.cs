using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public FormateurEntity Formateur { get; set; } = new() { Nom = string.Empty, Prenom = string.Empty };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var formateur = await context.Formateurs.FindAsync(id);
        if (formateur is null) return NotFound();
        Formateur = formateur;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var formateur = await context.Formateurs.FindAsync(Formateur.Id);
        if (formateur is null) return NotFound();
        formateur.UserId = Formateur.UserId;
        formateur.Nom = Formateur.Nom;
        formateur.Prenom = Formateur.Prenom;
        formateur.Bio = Formateur.Bio;
        formateur.Specialites = Formateur.Specialites;
        formateur.PhotoUrl = Formateur.PhotoUrl;
        formateur.EstActif = Formateur.EstActif;
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
