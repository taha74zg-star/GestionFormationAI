using AIFormationPlatform.Core.Constants;
using AIFormationPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIFormationPlatform.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Impossible de créer le rôle {role}.");
            }
        }

        if (await userManager.GetUsersInRoleAsync(RoleNames.Admin) is { Count: > 0 })
            return;

        var email = configuration["SeedAdmin:Email"] ?? "admin@aiformation.local";
        var password = configuration["SeedAdmin:Password"] ?? "ChangeMe!2026";
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Prenom = "Administrateur",
                Nom = "AIFormation",
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Impossible de créer l'administrateur initial : {string.Join(" ", createResult.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            if (!addRoleResult.Succeeded)
                throw new InvalidOperationException("Impossible d'attribuer le rôle Admin au compte initial.");
        }

        logger.LogWarning("Compte administrateur initial prêt : {Email}. Changez SeedAdmin:Password après la première connexion.", email);
    }
}
