using AIFormationPlatform.Core.Constants;
using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIFormationPlatform.Infrastructure.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "formateur.ia@aiformation.local";
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, Prenom = "Formateur", Nom = "IA", EmailConfirmed = true };
            var result = await users.CreateAsync(user, "ChangeMe!2026");
            if (!result.Succeeded) throw new InvalidOperationException("Création du Formateur IA impossible.");
        }
        if (!await users.IsInRoleAsync(user, RoleNames.Formateur)) await users.AddToRoleAsync(user, RoleNames.Formateur);
        if (!await db.Formateurs.AnyAsync(f => f.UserId == user.Id, ct)) db.Formateurs.Add(new Formateur { UserId = user.Id, Prenom = "Formateur", Nom = "IA", Specialites = "Intelligence artificielle", Bio = "Formateur virtuel Anam.ai" });
        if (!await db.Formations.AnyAsync(f => f.Titre == "Introduction à .NET", ct)) db.Formations.Add(new Formation { Titre = "Introduction à .NET", Description = "Les bases de .NET et ASP.NET Core.", Categorie = "Développement", Niveau = "Débutant", DureeEnMinutes = 180, Contenu = "Cours de démonstration .NET.", EstPubliee = true });
        if (!await db.Formations.AnyAsync(f => f.Titre == "Fondamentaux des réseaux", ct)) db.Formations.Add(new Formation { Titre = "Fondamentaux des réseaux", Description = "TCP/IP, DNS, routage et sécurité réseau.", Categorie = "Réseaux", Niveau = "Débutant", DureeEnMinutes = 150, Contenu = "Cours de démonstration réseau.", EstPubliee = true });
        await db.SaveChangesAsync(ct);
    }
}
