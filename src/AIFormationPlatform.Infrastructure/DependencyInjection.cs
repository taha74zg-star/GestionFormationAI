using AIFormationPlatform.Core.Constants;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Core.Options;
using AIFormationPlatform.Infrastructure.Data;
using AIFormationPlatform.Infrastructure.Identity;
using AIFormationPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIFormationPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AnamOptions>(options =>
        {
            configuration.GetSection(AnamOptions.SectionName).Bind(options);
            options.ApiKey = configuration["ANAM_API_KEY"] ?? options.ApiKey;
            options.AvatarId = configuration["ANAM_AVATAR_ID"] ?? options.AvatarId;
            options.VoiceId = configuration["ANAM_VOICE_ID"] ?? options.VoiceId;
            options.AvatarModel = configuration["ANAM_AVATAR_MODEL"] ?? options.AvatarModel;
            options.LlmId = configuration["ANAM_LLM_ID"] ?? options.LlmId;
            options.PersonaName = configuration["ANAM_PERSONA_NAME"] ?? options.PersonaName;
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpClient(nameof(AnamAvatarService));
        // Register both basic and advanced avatar service; keep existing AnamAvatarService as fallback
        services.AddScoped<IAvatarService, AnamAvatarAdvancedService>();

        // Formation service
        services.AddScoped<AIFormationPlatform.Core.Interfaces.IFormationService, AIFormationPlatform.Infrastructure.Services.FormationService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
