using System.Collections.Concurrent;

namespace AIFormationPlatform.Web.Features.Chat;

public class ChatService : IChatService
{
    private readonly OpenAIProvider _openAi;
    private readonly ILogger<ChatService> _logger;

    private static readonly ConcurrentDictionary<string, List<(string Role, string Content)>> _sessions = new();

    public ChatService(OpenAIProvider openAi, ILogger<ChatService> logger)
    {
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<string> AskAsync(string message, List<(string Role, string Content)> history, CancellationToken ct = default)
    {
        return await _openAi.GenerateAsync(ChatPrompts.SystemPrompt, history, ct);
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(string message, List<(string Role, string Content)> history, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var chunk in _openAi.GenerateStreamingAsync(ChatPrompts.SystemPrompt, history, ct))
        {
            yield return chunk;
        }
    }

    public static List<(string Role, string Content)> GetOrCreateSession(string sessionId)
    {
        return _sessions.GetOrAdd(sessionId, _ => new List<(string, string)>());
    }

    public static void AddMessage(string sessionId, string role, string content)
    {
        var session = GetOrCreateSession(sessionId);
        lock (session)
        {
            session.Add((role, content));
        }
    }

    public static void ClearSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public static void CleanupOldSessions()
    {
        var cutoff = DateTime.UtcNow.AddHours(2);
        foreach (var key in _sessions.Keys)
        {
            if (_sessions.TryGetValue(key, out var messages) && messages.Count == 0)
            {
                _sessions.TryRemove(key, out _);
            }
        }
    }
}
