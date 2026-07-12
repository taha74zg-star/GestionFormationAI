namespace AIFormationPlatform.Web.Features.AITrainer.Services;

public interface IAITrainerService
{
    Task<AITrainerResponse> AskQuestionAsync(
        string userId,
        int lessonId,
        string question,
        int? conversationId = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<StreamingChunk> AskQuestionStreamingAsync(
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

public class StreamingChunk
{
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public int ConversationId { get; set; }
}

public interface IAIProvider
{
    Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    Task<string> GenerateResponseWithHistoryAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string systemPrompt,
        List<(string Role, string Content)> history,
        CancellationToken cancellationToken = default);
}
