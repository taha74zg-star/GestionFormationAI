using OpenAI;
using OpenAI.Chat;

namespace AIFormationPlatform.Web.Features.Chat;

public class OpenAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProvider> _logger;

    public OpenAIProvider(IConfiguration configuration, ILogger<OpenAIProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken ct = default)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
            return "Service IA non configuré. Veuillez contacter l'administrateur.";

        try
        {
            var client = new OpenAIClient(apiKey).GetChatClient("gpt-4o-mini");
            var messages = BuildMessages(systemPrompt, history);
            var response = await client.CompleteChatAsync(messages, cancellationToken: ct);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur OpenAI");
            return "Désolé, une erreur est survenue. Veuillez réessayer.";
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var text = await CollectStreamingAsync(systemPrompt, history, ct);
        foreach (var chunk in SplitIntoChunks(text, 30))
        {
            yield return chunk;
        }
    }

    private async Task<string> CollectStreamingAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken ct)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
            return "Service IA non configuré.";

        OpenAIClient openAi;
        try
        {
            openAi = new OpenAIClient(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur création client OpenAI");
            return "Erreur de connexion au service IA.";
        }

        var chatClient = openAi.GetChatClient("gpt-4o-mini");
        var messages = BuildMessages(systemPrompt, history);

        try
        {
            var streaming = chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct);
            var sb = new System.Text.StringBuilder();
            await foreach (var update in streaming.WithCancellation(ct))
            {
                foreach (var part in update.ContentUpdate)
                {
                    sb.Append(part.Text);
                }
            }
            return sb.ToString();
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur streaming OpenAI");
            return "Erreur lors de la génération.";
        }
    }

    private static List<ChatMessage> BuildMessages(string systemPrompt, List<(string Role, string Content)> history)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var (role, content) in history)
        {
            messages.Add(role == "user"
                ? new UserChatMessage(content)
                : new AssistantChatMessage(content));
        }
        return messages;
    }

    private static IEnumerable<string> SplitIntoChunks(string text, int size)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        for (int i = 0; i < text.Length; i += size)
        {
            yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }
}
