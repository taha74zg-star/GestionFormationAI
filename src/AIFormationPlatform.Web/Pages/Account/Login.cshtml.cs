using System.ComponentModel.DataAnnotations;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Identifiants invalides.");
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Ce compte est temporairement verrouillé." : "Identifiants invalides.");
            return Page();
        }

        var roles = await userManager.GetRolesAsync(user);
        return LocalRedirect(GetRoleLandingPage(roles));
    }

    private static string GetRoleLandingPage(IList<string> roles) =>
        roles.Contains("Admin") ? "/Admin/Dashboard" :
        roles.Contains("Formateur") ? "/Formateur/Dashboard" : "/Apprenant/MesFormations";

    public sealed class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
