using System.ComponentModel.DataAnnotations;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ComptesModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signInManager) : PageModel
{
    public IList<ApplicationUser> Comptes { get; private set; } = [];
    [BindProperty] public InputModel Input { get; set; } = new();
    public void OnGet() => Comptes = users.Users.OrderBy(u => u.Email).ToList();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { OnGet(); return Page(); }
        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email, Prenom = Input.Prenom, Nom = Input.Nom, EmailConfirmed = true };
        var result = await users.CreateAsync(user, Input.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); OnGet(); return Page(); }
        await users.AddToRoleAsync(user, Input.Role); TempData["Success"] = "Compte créé."; return RedirectToPage();
    }
    public async Task<IActionResult> OnPostStartAiTrainerAsync()
    {
        var aiTrainer = await users.FindByEmailAsync("formateur.ia@aiformation.local");
        if (aiTrainer is null) { TempData["Error"] = "Le Formateur IA sera créé au prochain redémarrage."; return RedirectToPage(); }
        await signInManager.SignOutAsync();
        await signInManager.SignInAsync(aiTrainer, false);
        return RedirectToPage("/Formateur/Dashboard");
    }
    public sealed class InputModel { [Required] public string Prenom { get; set; } = ""; [Required] public string Nom { get; set; } = ""; [Required, EmailAddress] public string Email { get; set; } = ""; [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string Password { get; set; } = ""; [Required] public string Role { get; set; } = "Apprenant"; }
}
