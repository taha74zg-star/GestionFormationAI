using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class DetailsModel(ApplicationDbContext context) : PageModel
{
    public Categorie Categorie { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categorie = await context.Categories.Include(c => c.Parent).SingleOrDefaultAsync(c => c.Id == id);
        if (categorie is null) return NotFound();
        Categorie = categorie;
        return Page();
    }
}
