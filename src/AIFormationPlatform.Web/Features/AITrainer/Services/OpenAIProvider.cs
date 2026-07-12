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
}
