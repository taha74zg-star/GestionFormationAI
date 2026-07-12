using OpenAI;
using OpenAI.Chat;

namespace AIFormationPlatform.Web.Features.AITrainer.Services;

public class OpenAIProvider : IAIProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProvider> _logger;

    public OpenAIProvider(IConfiguration configuration, ILogger<OpenAIProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY n'est pas configurée");
            return "Le service d'intelligence artificielle n'est pas configuré. Veuillez contacter l'administrateur.";
        }

        try
        {
            var openAi = new OpenAIClient(apiKey);
            var chatClient = openAi.GetChatClient("gpt-4o-mini");

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel à l'API OpenAI");
            throw;
        }
    }

    public async Task<string> GenerateResponseWithHistoryAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY n'est pas configurée");
            return "Le service d'intelligence artificielle n'est pas configuré.";
        }

        try
        {
            var openAi = new OpenAIClient(apiKey);
            var chatClient = openAi.GetChatClient("gpt-4o-mini");

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            foreach (var (role, content) in history)
            {
                if (role == "User")
                    messages.Add(new UserChatMessage(content));
                else if (role == "Assistant")
                    messages.Add(new AssistantChatMessage(content));
            }

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel à l'API OpenAI (avec historique)");
            throw;
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY n'est pas configurée");
            yield return "Le service d'intelligence artificielle n'est pas configuré.";
            yield break;
        }

        var openAi = CreateClient(apiKey);
        if (openAi == null)
        {
            yield return "Erreur de connexion au service IA.";
            yield break;
        }

        var client = openAi.GetChatClient("gpt-4o-mini");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        foreach (var (role, content) in history)
        {
            if (role == "User")
                messages.Add(new UserChatMessage(content));
            else if (role == "Assistant")
                messages.Add(new AssistantChatMessage(content));
        }

        var text = await CompleteStreamingAsync(client, messages, cancellationToken);
        foreach (var chunk in text.SplitIntoChunks(30))
        {
            yield return chunk;
        }
    }

    private OpenAIClient? CreateClient(string apiKey)
    {
        try
        {
            return new OpenAIClient(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du client OpenAI");
            return null;
        }
    }

    private async Task<string> CompleteStreamingAsync(
        ChatClient client,
        List<ChatMessage> messages,
        CancellationToken ct)
    {
        try
        {
            var streamingResponse = client.CompleteChatStreamingAsync(messages, cancellationToken: ct);
            var sb = new System.Text.StringBuilder();

            await foreach (var update in streamingResponse.WithCancellation(ct))
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    sb.Append(contentPart.Text);
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
            _logger.LogError(ex, "Erreur lors du streaming OpenAI");
            return "Erreur lors de la génération de la réponse.";
        }
    }
}

internal static class StringChunkExtensions
{
    internal static IEnumerable<string> SplitIntoChunks(this string text, int chunkSize)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
        }
    }
}
