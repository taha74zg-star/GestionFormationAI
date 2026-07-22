using System.ComponentModel.DataAnnotations;
using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Data;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FormateurEntity = AIFormationPlatform.Core.Entities.Formateur;

namespace AIFormationPlatform.Web.Pages.Admin.Formateurs;

[Authorize(Roles = "Admin")]
public class CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty] public FormateurEntity Formateur { get; set; } = new() { Nom = string.Empty, Prenom = string.Empty };
    [BindProperty] public AccountInput Account { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (!string.IsNullOrWhiteSpace(Account.ExistingEmail))
        {
            var user = await userManager.FindByEmailAsync(Account.ExistingEmail);
            if (user is null) { ModelState.AddModelError("Account.ExistingEmail", "Aucun compte ne correspond à cet e-mail."); return Page(); }
            Formateur.UserId = user.Id;
            if (!await userManager.IsInRoleAsync(user, "Formateur")) await userManager.AddToRoleAsync(user, "Formateur");
        }
        else if (Account.CreateAccount)
        {
            if (string.IsNullOrWhiteSpace(Account.NewEmail) || string.IsNullOrWhiteSpace(Account.Password)) { ModelState.AddModelError(string.Empty, "Indiquez l'e-mail et le mot de passe du nouveau compte."); return Page(); }
            var user = new ApplicationUser { UserName = Account.NewEmail, Email = Account.NewEmail, Prenom = Formateur.Prenom, Nom = Formateur.Nom };
            var result = await userManager.CreateAsync(user, Account.Password);
            if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return Page(); }
            await userManager.AddToRoleAsync(user, "Formateur"); Formateur.UserId = user.Id;
        }
        context.Formateurs.Add(Formateur); await context.SaveChangesAsync(); TempData["Success"] = "Formateur créé."; return RedirectToPage("Index");
    }
    public sealed class AccountInput { [EmailAddress] public string? ExistingEmail { get; set; } public bool CreateAccount { get; set; } [EmailAddress] public string? NewEmail { get; set; } [DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string? Password { get; set; } }
}
