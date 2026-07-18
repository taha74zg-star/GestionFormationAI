using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Formateur;

[Authorize(Roles = "Formateur")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
