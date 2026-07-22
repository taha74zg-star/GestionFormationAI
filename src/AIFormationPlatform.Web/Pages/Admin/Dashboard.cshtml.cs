using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}
