using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIFormationPlatform.Web.Features.Chat;

var builder = WebApplication.CreateBuilder(args);

var appPort = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{appPort}");

builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseStaticFiles();

app.MapPost("/api/chat", async (ChatRequest req, IChatService chatService, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "Message requis." });

    var sessionId = req.SessionId;
    if (string.IsNullOrEmpty(sessionId))
    {
        sessionId = Guid.NewGuid().ToString("N");
    }

    var history = ChatService.GetOrCreateSession(sessionId);
    ChatService.AddMessage(sessionId, "user", req.Message);

    http.Response.Headers.Append("Content-Type", "text/event-stream");
    http.Response.Headers.Append("Cache-Control", "no-cache");
    http.Response.Headers.Append("Connection", "keep-alive");

    var sb = new StringBuilder();
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

app.MapPost("/api/session-token", async (IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    var anamApiKey = config["ANAM_API_KEY"];
    if (string.IsNullOrEmpty(anamApiKey))
        return Results.BadRequest(new { error = "Clé API Anam non configurée." });

    try
    {
        var client = httpClientFactory.CreateClient();
        var payload = new
        {
            personaConfig = new
            {
                name = "Assistant IA",
                avatarId = "30fa96d0-26c4-4e55-94a0-517025942e18",
                avatarModel = "cara-4",
                voiceId = "6bfbe25a-979d-40f3-a92b-5394170af54b",
                llmId = "CUSTOMER_CLIENT_V1",
                systemPrompt = "Tu es un assistant IA intelligent et amical. Réponds toujours dans la langue de l'utilisateur."
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anam.ai/v1/auth/session-token")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anamApiKey);

        var response = await client.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Results.BadRequest(new { error = "Impossible de créer la session avatar." });

        var tokenData = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var sessionToken = tokenData.GetProperty("sessionToken").GetString();

        return Results.Json(new { sessionToken });
    }
    catch
    {
        return Results.BadRequest(new { error = "Erreur de connexion au service avatar." });
    }
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
