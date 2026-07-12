using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class AIMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public AIConversation Conversation { get; set; } = null!;
}
