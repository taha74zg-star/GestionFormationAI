using System.ComponentModel.DataAnnotations;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Account;

[AllowAnonymous]
public class RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    private static readonly string[] AllowedRoles = ["Formateur", "Apprenant"];

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (!AllowedRoles.Contains(Input.Role))
        {
            ModelState.AddModelError("Input.Role", "Le rôle sélectionné est invalide.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            Prenom = Input.Prenom,
            Nom = Input.Nom
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var roleResult = await userManager.AddToRoleAsync(user, Input.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Input.Role == "Formateur" ? "/Formateur" : "/Apprenant");
    }

    public sealed class RegisterInput
    {
        [Required, StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Apprenant";
    }
}
