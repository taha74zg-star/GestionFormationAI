using System.Net.Http.Json;
using System.Text.Json;

namespace AIFormationPlatform.Web.Features.Chat;

public class OpenAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly HttpClient _httpClient;

    public OpenAIProvider(
        IConfiguration configuration,
        ILogger<OpenAIProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken ct = default)
    {
        return await GenerateWithGeminiAsync(systemPrompt, history, ct);
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken ct = default)
    {
        var text = await GenerateWithGeminiAsync(systemPrompt, history, ct);

        foreach (var chunk in SplitIntoChunks(text, 30))
        {
            yield return chunk;
        }
    }

    private async Task<string> GenerateWithGeminiAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken ct)
    {
        var apiKey = _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
            return "Service IA Gemini non configuré.";

        try
        {
            var contents = new List<object>();

            foreach (var (role, content) in history)
            {
                contents.Add(new
                {
                    role = role == "user" ? "user" : "model",
                    parts = new[]
                    {
                        new { text = content }
                    }
                });
            }

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                },

                contents
            };

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken: ct);

            var rawResponse = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Erreur Gemini HTTP {StatusCode}: {Response}",
                    response.StatusCode,
                    rawResponse);

                return $"Erreur Gemini ({(int)response.StatusCode}).";
            }

            using var json = JsonDocument.Parse(rawResponse);

            if (!json.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning(
                    "Gemini n'a retourné aucun candidat: {Response}",
                    rawResponse);

                return "Gemini n'a retourné aucune réponse.";
            }

            var candidate = candidates[0];

            if (!candidate.TryGetProperty("content", out var responseContent)
                || !responseContent.TryGetProperty("parts", out var parts))
            {
                return "Réponse Gemini invalide.";
            }

            var result = new System.Text.StringBuilder();

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    result.Append(text.GetString());
                }
            }

            return result.Length > 0
                ? result.ToString()
                : "Gemini n'a retourné aucun texte.";
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur Gemini");
            return "Erreur lors de la génération Gemini.";
        }
    }

    private static IEnumerable<string> SplitIntoChunks(string text, int size)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        for (var i = 0; i < text.Length; i += size)
        {
            yield return text.Substring(
                i,
                Math.Min(size, text.Length - i));
        }
    }
}
