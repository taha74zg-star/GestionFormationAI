using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Categorie Categorie { get; set; } = new() { Nom = string.Empty };

    public SelectList Parents { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categorie = await context.Categories.FindAsync(id);
        if (categorie is null) return NotFound();
        Categorie = categorie;
        await LoadParentsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Categorie.ParentId == Categorie.Id)
            ModelState.AddModelError("Categorie.ParentId", "Une catégorie ne peut pas être sa propre parente.");
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(Categorie.Id);
            return Page();
        }

        var categorie = await context.Categories.FindAsync(Categorie.Id);
        if (categorie is null) return NotFound();
        categorie.Nom = Categorie.Nom;
        categorie.Description = Categorie.Description;
        categorie.ParentId = Categorie.ParentId;
        categorie.Ordre = Categorie.Ordre;
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadParentsAsync(int currentId) =>
        Parents = new SelectList(await context.Categories.Where(c => c.Id != currentId).OrderBy(c => c.Nom).ToListAsync(), nameof(Categorie.Id), nameof(Categorie.Nom));
}
