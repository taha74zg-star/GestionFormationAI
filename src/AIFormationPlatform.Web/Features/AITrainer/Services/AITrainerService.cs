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

            var conversation = conversationId.HasValue
                ? await _context.AIConversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId, cancellationToken)
                : null;

            if (conversation == null)
            {
                conversation = new AIConversation
                {
                    UserId = userId,
                    LessonId = lessonId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AIConversations.Add(conversation);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _context.AIMessages.Add(new AIMessage
            {
                ConversationId = conversation.Id,
                Role = "User",
                Content = question,
                SentAt = DateTime.UtcNow
            });

            var systemPrompt = AITrainerPrompts.GetSystemPrompt(lesson.Content);
            var userPrompt = AITrainerPrompts.GetUserPrompt(question);

            var response = await _aiProvider.GenerateResponseAsync(systemPrompt, userPrompt, cancellationToken);

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
}
