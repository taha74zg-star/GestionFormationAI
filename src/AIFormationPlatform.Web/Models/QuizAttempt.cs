using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class QuizAttempt
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int QuizId { get; set; }

    public int Score { get; set; }

    public int TotalPoints { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
