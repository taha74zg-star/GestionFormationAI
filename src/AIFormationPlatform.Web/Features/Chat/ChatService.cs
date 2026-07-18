using System.Collections.Concurrent;

namespace AIFormationPlatform.Web.Features.Chat;

public class ChatService : IChatService
{
    private readonly OpenAIProvider _openAi;
    private readonly ILogger<ChatService> _logger;

    private static readonly ConcurrentDictionary<string, SessionState> _sessions = new();

    public ChatService(OpenAIProvider openAi, ILogger<ChatService> logger)
    {
        _openAi = openAi;
        _logger = logger;
    }

    public async Task<string> AskAsync(string message, List<(string Role, string Content)> history, string? systemPrompt = null, CancellationToken ct = default)
    {
        return await _openAi.GenerateAsync(systemPrompt ?? ChatPrompts.SystemPrompt, history, ct);
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(string message, List<(string Role, string Content)> history, string? systemPrompt = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var chunk in _openAi.GenerateStreamingAsync(systemPrompt ?? ChatPrompts.SystemPrompt, history, ct))
        {
            yield return chunk;
        }
    }

    public static List<(string Role, string Content)> GetOrCreateSession(string sessionId)
    {
        CleanupOldSessions();
        var state = _sessions.GetOrAdd(sessionId, _ => new SessionState());
        state.LastActivityUtc = DateTime.UtcNow;
        return state.Messages;
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
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var (key, state) in _sessions)
        {
            if (state.LastActivityUtc < cutoff)
                _sessions.TryRemove(key, out _);
        }
    }

    private sealed class SessionState
    {
        public List<(string Role, string Content)> Messages { get; } = new();
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
    }
}
