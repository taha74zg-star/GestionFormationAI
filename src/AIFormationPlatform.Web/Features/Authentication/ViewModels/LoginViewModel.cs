using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.Authentication.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Se souvenir de moi")]
    public bool RememberMe { get; set; }
}
