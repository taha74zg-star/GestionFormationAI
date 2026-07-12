using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class Lesson
{
    public int Id { get; set; }

    public int ModuleId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Module Module { get; set; } = null!;
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<AIConversation> AIConversations { get; set; } = new List<AIConversation>();
}
