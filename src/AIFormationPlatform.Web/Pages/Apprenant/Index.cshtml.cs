using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Apprenant;

[Authorize(Roles = "Apprenant")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
