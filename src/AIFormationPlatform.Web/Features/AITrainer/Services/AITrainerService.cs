using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.AITrainer.Prompts;
using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.AITrainer.Services;

public class AITrainerService : IAITrainerService
{
    private readonly AppDbContext _context;
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<AITrainerService> _logger;

    public AITrainerService(
        AppDbContext context,
        IAIProvider aiProvider,
        ILogger<AITrainerService> logger)
    {
        _context = context;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<AITrainerResponse> AskQuestionAsync(
        string userId,
        int lessonId,
        string question,
        int? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

            if (lesson == null)
            {
                return new AITrainerResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Leçon introuvable."
                };
            }

            var conversation = await GetOrCreateConversationAsync(userId, lessonId, conversationId, cancellationToken);

            _context.AIMessages.Add(new AIMessage
            {
                ConversationId = conversation.Id,
                Role = "User",
                Content = question,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            var history = await GetConversationHistoryAsync(conversation.Id, cancellationToken);
            var systemPrompt = AITrainerPrompts.GetSystemPrompt(lesson.Content);
            var response = await _aiProvider.GenerateResponseWithHistoryAsync(systemPrompt, history, cancellationToken);

            _context.AIMessages.Add(new AIMessage
            {
                ConversationId = conversation.Id,
                Role = "Assistant",
                Content = response,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            return new AITrainerResponse
            {
                ConversationId = conversation.Id,
                Response = response,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel au Formateur IA pour la leçon {LessonId}", lessonId);
            return new AITrainerResponse
            {
                IsSuccess = false,
                ErrorMessage = "Le Formateur IA est temporairement indisponible. Veuillez réessayer ultérieurement."
            };
        }
    }

    public async IAsyncEnumerable<StreamingChunk> AskQuestionStreamingAsync(
        string userId,
        int lessonId,
        string question,
        int? conversationId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson == null)
        {
            yield return new StreamingChunk { Content = "Leçon introuvable.", IsComplete = true };
            yield break;
        }

        var conversation = await GetOrCreateConversationAsync(userId, lessonId, conversationId, cancellationToken);

        _context.AIMessages.Add(new AIMessage
        {
            ConversationId = conversation.Id,
            Role = "User",
            Content = question,
            SentAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        var history = await GetConversationHistoryAsync(conversation.Id, cancellationToken);
        var systemPrompt = AITrainerPrompts.GetSystemPrompt(lesson.Content);

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var chunk in _aiProvider.GenerateStreamingResponseAsync(systemPrompt, history, cancellationToken))
        {
            fullResponse.Append(chunk);
            yield return new StreamingChunk
            {
                Content = chunk,
                IsComplete = false,
                ConversationId = conversation.Id
            };
        }

        _context.AIMessages.Add(new AIMessage
        {
            ConversationId = conversation.Id,
            Role = "Assistant",
            Content = fullResponse.ToString(),
            SentAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        yield return new StreamingChunk
        {
            Content = string.Empty,
            IsComplete = true,
            ConversationId = conversation.Id
        };
    }

    private async Task<AIConversation> GetOrCreateConversationAsync(
        string userId, int lessonId, int? conversationId, CancellationToken ct)
    {
        if (conversationId.HasValue)
        {
            var existing = await _context.AIConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId, ct);
            if (existing != null) return existing;
        }

        var conversation = new AIConversation
        {
            UserId = userId,
            LessonId = lessonId,
            CreatedAt = DateTime.UtcNow
        };
        _context.AIConversations.Add(conversation);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    private async Task<List<(string Role, string Content)>> GetConversationHistoryAsync(
        int conversationId, CancellationToken ct)
    {
        return await _context.AIMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new ValueTuple<string, string>(m.Role, m.Content))
            .ToListAsync(ct);
    }
}
