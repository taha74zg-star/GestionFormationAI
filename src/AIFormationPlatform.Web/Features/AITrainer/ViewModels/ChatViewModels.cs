using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.AITrainer.ViewModels;

public class ChatRequestViewModel
{
    [Required(ErrorMessage = "La question est requise")]
    public string Question { get; set; } = string.Empty;

    public int LessonId { get; set; }

    public int? ConversationId { get; set; }
}

public class ChatMessageViewModel
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class ChatResponseViewModel
{
    public int ConversationId { get; set; }
    public string Response { get; set; } = string.Empty;
    public List<ChatMessageViewModel> History { get; set; } = new();
}
