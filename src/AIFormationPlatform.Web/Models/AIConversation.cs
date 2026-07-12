using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class AIConversation
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int LessonId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
    public ICollection<AIMessage> Messages { get; set; } = new List<AIMessage>();
}
