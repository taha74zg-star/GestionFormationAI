namespace AIFormationPlatform.Web.Features.Chat;

public interface IChatService
{
    Task<string> AskAsync(string message, List<(string Role, string Content)> history, CancellationToken ct = default);
    IAsyncEnumerable<string> AskStreamingAsync(string message, List<(string Role, string Content)> history, CancellationToken ct = default);
}
