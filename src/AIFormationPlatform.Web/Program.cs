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
    port = 8080;

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
    await DemoDataSeeder.SeedAsync(migrationScope.ServiceProvider);
}
catch (Exception exception)
{
    app.Logger.LogCritical(
        exception,
        "Impossible de se connecter Ã  la base de donnÃ©es ou d'appliquer les migrations EF au dÃ©marrage.");
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

    string? modulePrompt = null;
    if (req.ModuleId is not null)
    {
        var contextResult = await GetModuleContextAsync(
            http.RequestServices.GetRequiredService<ApplicationDbContext>(),
            http.User,
            req.ModuleId.Value,
            http.RequestAborted);
        if (contextResult.Error is not null)
            return contextResult.Error;
        modulePrompt = contextResult.Prompt;
    }

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
        await foreach (var chunk in chatService.AskStreamingAsync(req.Message, history, modulePrompt, http.RequestAborted))
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
    string? modulePrompt = null;
    if (req?.ModuleId is not null)
    {
        var contextResult = await GetModuleContextAsync(dbContext, user, req.ModuleId.Value, cancellationToken);
        if (contextResult.Error is not null)
            return contextResult.Error;
        modulePrompt = contextResult.Prompt;
    }

    var script = modulePrompt ?? string.Empty;
    AvatarPersonaConfig? persona = string.IsNullOrWhiteSpace(script)
        ? null
        : new AvatarPersonaConfig(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, script);

    // Use StartSessionAsync to allow providing a script/fallback
    var start = await avatarService.StartSessionAsync(script, persona, cancellationToken);

    if (!start.Success)
    {
        // Fallback: return fallback text so client can display text content
        return Results.Json(
            new { sessionToken = (string?)null, fallback = start.FallbackText, error = start.ErrorMessage },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new { sessionToken = start.SessionToken });
});

app.MapPost("/api/clear-session", (ChatClearRequest req) =>
{
    if (!string.IsNullOrEmpty(req.SessionId))
        ChatService.ClearSession(req.SessionId);
    return Results.Ok(new { cleared = true });
}).RequireAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

static async Task<(string? Prompt, IResult? Error)> GetModuleContextAsync(
    ApplicationDbContext dbContext,
    ClaimsPrincipal user,
    int moduleId,
    CancellationToken cancellationToken)
{
    if (!user.IsInRole("Apprenant"))
        return (null, Results.Forbid());

    var apprenantId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var module = await dbContext.Modules
        .Where(m => m.Id == moduleId)
        .Select(m => new { m.FormationId, m.Titre, m.ContenuTexte })
        .SingleOrDefaultAsync(cancellationToken);
    if (module is null)
        return (null, Results.NotFound());

    var estInscrit = await dbContext.Inscriptions.AnyAsync(
        i => i.ApprenantId == apprenantId && i.FormationId == module.FormationId,
        cancellationToken);
    if (!estInscrit)
        return (null, Results.Forbid());

    var prompt = $"""
        Tu presentes le module suivant et reponds uniquement aux questions qui concernent son contenu.
        Parle en francais par defaut. Si l'apprenant demande explicitement une autre langue, reponds dans cette langue jusqu'a ce qu'il demande un nouveau changement.

        Titre du module : {module.Titre}

        Contenu du cours :
        {module.ContenuTexte ?? "Aucun contenu texte n'est disponible pour ce module."}
        """;
    return (prompt, null);
}

public record ChatClearRequest(string SessionId);

public record AvatarTokenRequest(int? ModuleId = null);
