using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AIFormationPlatform.Web.Features.AITrainer.Services;
using AIFormationPlatform.Web.Features.AITrainer.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIFormationPlatform.Web.Features.AITrainer.Controllers;

[Authorize]
public class AITrainerController : Controller
{
    private readonly IAITrainerService _aiTrainerService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AITrainerController> _logger;

    public AITrainerController(
        IAITrainerService aiTrainerService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AITrainerController> logger)
    {
        _aiTrainerService = aiTrainerService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequestViewModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var response = await _aiTrainerService.AskQuestionAsync(
            userId,
            request.LessonId,
            request.Question,
            request.ConversationId);

        if (!response.IsSuccess)
        {
            return Json(new { success = false, message = response.ErrorMessage });
        }

        return Json(new
        {
            success = true,
            response = response.Response,
            conversationId = response.ConversationId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SessionToken([FromBody] SessionTokenRequest request)
    {
        var anamApiKey = _configuration["ANAM_API_KEY"];
        if (string.IsNullOrEmpty(anamApiKey))
        {
            return BadRequest(new { success = false, message = "Clé API Anam non configurée." });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                personaConfig = new
                {
                    name = "Formateur IA",
                    avatarId = "30fa96d0-26c4-4e55-94a0-517025942e18",
                    avatarModel = "cara-4",
                    voiceId = "6bfbe25a-979d-40f3-a92b-5394170af54b",
                    llmId = "CUSTOMER_CLIENT_V1",
                    systemPrompt = "Tu es un formateur IA patient et pédagogue. Réponds toujours en français."
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anam.ai/v1/auth/session-token")
            {
                Content = content
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anamApiKey);

            var response = await client.SendAsync(requestMessage);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anam session token failed: {Status} - {Response}", response.StatusCode, responseJson);
                return BadRequest(new { success = false, message = "Impossible de créer la session avatar." });
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var sessionToken = tokenData.GetProperty("sessionToken").GetString();

            return Json(new { success = true, sessionToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du token Anam");
            return BadRequest(new { success = false, message = "Erreur de connexion au service avatar." });
        }
    }

    [HttpPost]
    public async Task Stream([FromBody] ChatRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Requête invalide.");
            return;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var chunk in _aiTrainerService.AskQuestionStreamingAsync(
                userId, request.LessonId, request.Question, request.ConversationId,
                HttpContext.RequestAborted))
            {
                var data = JsonSerializer.Serialize(new
                {
                    content = chunk.Content,
                    complete = chunk.IsComplete,
                    conversationId = chunk.ConversationId
                });

                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du streaming");
            var errorData = JsonSerializer.Serialize(new { content = "Erreur de streaming.", complete = true });
            await Response.WriteAsync($"data: {errorData}\n\n");
        }
    }
}

public class SessionTokenRequest
{
    public int LessonId { get; set; }
}
