using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class LessonProgress
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int LessonId { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
