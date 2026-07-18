using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class DeleteModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Categorie Categorie { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categorie = await context.Categories.FindAsync(id);
        if (categorie is null) return NotFound();
        Categorie = categorie;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var categorie = await context.Categories.FindAsync(Categorie.Id);
        if (categorie is null) return NotFound();

        if (await context.Categories.AnyAsync(c => c.ParentId == categorie.Id))
        {
            ModelState.AddModelError(string.Empty, "Cette catégorie contient des sous-catégories et ne peut pas être supprimée.");
            Categorie = categorie;
            return Page();
        }

        context.Categories.Remove(categorie);
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
