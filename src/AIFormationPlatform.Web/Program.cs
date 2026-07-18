using System.Text.Json;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Infrastructure;
using AIFormationPlatform.Infrastructure.Data;
using AIFormationPlatform.Infrastructure.Identity;
using AIFormationPlatform.Web.Features.Chat;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("La variable ConnectionStrings__DefaultConnection est requise.");

var appPort = Environment.GetEnvironmentVariable("PORT");
if (!int.TryParse(appPort, out var port) || port is < 1 or > 65535)
    throw new InvalidOperationException("La variable PORT doit contenir un port valide.");

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddHttpClient();
builder.Services.AddInfrastructure(builder.Configuration);

// Add MVC support for Admin area (Razor views)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

try
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
catch (Exception exception)
{
    app.Logger.LogCritical(
        exception,
        "Impossible de se connecter à la base de données ou d'appliquer les migrations EF au démarrage.");
    throw;
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Formateur", "Apprenant" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/chat", async (ChatRequest req, IChatService chatService, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "Message requis." });

    var sessionId = req.SessionId;
    if (string.IsNullOrEmpty(sessionId))
        sessionId = Guid.NewGuid().ToString("N");

    var history = ChatService.GetOrCreateSession(sessionId);
    ChatService.AddMessage(sessionId, "user", req.Message);

    http.Response.Headers.Append("Content-Type", "text/event-stream");
    http.Response.Headers.Append("Cache-Control", "no-cache");
    http.Response.Headers.Append("Connection", "keep-alive");

    var sb = new System.Text.StringBuilder();
    try
    {
        await foreach (var chunk in chatService.AskStreamingAsync(req.Message, history, http.RequestAborted))
        {
            sb.Append(chunk);
            var data = JsonSerializer.Serialize(new { content = chunk, sessionId });
            await http.Response.WriteAsync($"data: {data}\n\n", http.RequestAborted);
            await http.Response.Body.FlushAsync(http.RequestAborted);
        }
    }
    catch (OperationCanceledException) { }

    var fullReply = sb.ToString();
    ChatService.AddMessage(sessionId, "assistant", fullReply);

    var done = JsonSerializer.Serialize(new { content = "", done = true, sessionId });
    await http.Response.WriteAsync($"data: {done}\n\n");
    await http.Response.Body.FlushAsync();

    return Results.Empty;
});

app.MapPost("/api/session-token", async (
    IAvatarService avatarService,
    ApplicationDbContext dbContext,
    ClaimsPrincipal user,
    AvatarTokenRequest? req,
    CancellationToken cancellationToken) =>
{
    if (req?.ModuleId is not null)
    {
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Apprenant"))
            return Results.Forbid();

        var apprenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var module = await dbContext.Modules
            .Where(m => m.Id == req.ModuleId.Value)
            .Select(m => new { m.Id, m.FormationId, m.Titre, m.ContenuTexte })
            .SingleOrDefaultAsync(cancellationToken);
        if (module is null)
            return Results.NotFound();

        var estInscrit = await dbContext.Inscriptions.AnyAsync(
            i => i.ApprenantId == apprenantId && i.FormationId == module.FormationId,
            cancellationToken);
        if (!estInscrit)
            return Results.Forbid();

        var contexteModule = $"""
            Tu présentes le module suivant et réponds uniquement aux questions qui concernent son contenu.

            Titre du module : {module.Titre}

            Contenu du cours :
            {module.ContenuTexte ?? "Aucun contenu texte n'est disponible pour ce module."}
            """;
        req = req with { SystemPrompt = contexteModule };
    }

    AvatarPersonaConfig? persona = req is not null
        ? new AvatarPersonaConfig(
            req.PersonaName ?? string.Empty,
            req.AvatarId ?? string.Empty,
            req.AvatarModel ?? string.Empty,
            req.VoiceId ?? string.Empty,
            req.LlmId ?? string.Empty,
            req.SystemPrompt ?? string.Empty)
        : null;

    // Use StartSessionAsync to allow providing a script/fallback
    var script = req?.SystemPrompt ?? string.Empty;
    var start = await avatarService.StartSessionAsync(script, persona, cancellationToken);

    if (!start.Success)
    {
        // Fallback: return fallback text so client can display text content
        return Results.Json(new { sessionToken = (string?)null, fallback = start.FallbackText, error = start.ErrorMessage });
    }

    return Results.Json(new { sessionToken = start.SessionToken });
});

app.MapPost("/api/clear-session", (ChatClearRequest req) =>
{
    if (!string.IsNullOrEmpty(req.SessionId))
        ChatService.ClearSession(req.SessionId);
    return Results.Ok(new { cleared = true });
});

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public record ChatClearRequest(string SessionId);

public record AvatarTokenRequest(
    string? SystemPrompt = null,
    string? PersonaName = null,
    string? AvatarId = null,
    string? AvatarModel = null,
    string? VoiceId = null,
    string? LlmId = null,
    int? ModuleId = null);
