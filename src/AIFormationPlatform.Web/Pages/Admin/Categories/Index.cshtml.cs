using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Categories;

[Authorize(Roles = "Admin")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Categorie> Categories { get; private set; } = [];

    public async Task OnGetAsync() =>
        Categories = await context.Categories.Include(c => c.Parent).OrderBy(c => c.Ordre).ThenBy(c => c.Nom).ToListAsync();
}
