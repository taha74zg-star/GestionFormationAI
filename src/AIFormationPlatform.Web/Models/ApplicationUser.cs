using Microsoft.AspNetCore.Identity;

namespace AIFormationPlatform.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<AIConversation> AIConversations { get; set; } = new List<AIConversation>();
}
