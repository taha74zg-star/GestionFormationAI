using Microsoft.AspNetCore.Identity;

namespace AIFormationPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? Prenom { get; set; }
    public string? Nom { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
