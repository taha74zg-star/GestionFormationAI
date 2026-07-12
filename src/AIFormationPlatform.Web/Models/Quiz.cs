using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class Quiz
{
    public int Id { get; set; }

    public int FormationId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int TimeLimitMinutes { get; set; }

    public Formation Formation { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}
