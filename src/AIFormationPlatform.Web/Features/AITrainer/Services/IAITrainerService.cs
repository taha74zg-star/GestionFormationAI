namespace AIFormationPlatform.Web.Features.AITrainer.Services;

public interface IAITrainerService
{
    Task<AITrainerResponse> AskQuestionAsync(
        string userId,
        int lessonId,
        string question,
        int? conversationId = null,
        CancellationToken cancellationToken = default);
}

public class AITrainerResponse
{
    public int ConversationId { get; set; }
    public string Response { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAIProvider
{
    Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
