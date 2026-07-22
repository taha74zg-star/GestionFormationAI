using AIFormationPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class IndexModel(IAdminCatalogService catalog) : PageModel
{
    public PagedResult<FormateurSummary> Result { get; private set; } = new([], 1, 10, 0);
    public string? Search { get; private set; }
    public string? Sort { get; private set; }
    public async Task OnGetAsync(string? search, string? sort, int page = 1) { Search = search; Sort = sort; Result = await catalog.GetFormateursAsync(search, sort, page, 10); }
    public async Task<IActionResult> OnPostDeleteAsync(int id) { var result = await catalog.DeleteFormateurAsync(id); TempData[result.Deleted ? "Success" : "Error"] = result.Deleted ? "Formateur supprimé." : result.Error; return RedirectToPage(); }
}
