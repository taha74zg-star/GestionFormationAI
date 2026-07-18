using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Categorie Categorie { get; set; } = new() { Nom = string.Empty };

    public SelectList Parents { get; private set; } = default!;

    public async Task OnGetAsync() => await LoadParentsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync();
            return Page();
        }

        context.Categories.Add(Categorie);
        await context.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadParentsAsync() =>
        Parents = new SelectList(await context.Categories.OrderBy(c => c.Nom).ToListAsync(), nameof(Categorie.Id), nameof(Categorie.Nom));
}
