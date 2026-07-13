using System.Text.Json;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Infrastructure;
using AIFormationPlatform.Web.Features.Chat;

var builder = WebApplication.CreateBuilder(args);

var appPort = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{appPort}");

builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddHttpClient();
builder.Services.AddInfrastructure(builder.Configuration);

// Add MVC support for Admin area (Razor views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseStaticFiles();

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
    AvatarTokenRequest? req,
    CancellationToken cancellationToken) =>
{
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

app.MapFallbackToFile("index.html");

app.Run();

public record ChatClearRequest(string SessionId);

public record AvatarTokenRequest(
    string? SystemPrompt = null,
    string? PersonaName = null,
    string? AvatarId = null,
    string? AvatarModel = null,
    string? VoiceId = null,
    string? LlmId = null);
