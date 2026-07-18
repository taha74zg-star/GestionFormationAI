using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public FormateurEntity Formateur { get; set; } = new() { Nom = string.Empty, Prenom = string.Empty };

    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        context.Formateurs.Add(Formateur);
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
