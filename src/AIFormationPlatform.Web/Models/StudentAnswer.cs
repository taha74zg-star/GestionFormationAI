using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class StudentAnswer
{
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }

    public int QuestionId { get; set; }

    public int AnswerChoiceId { get; set; }

    public QuizAttempt QuizAttempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public AnswerChoice AnswerChoice { get; set; } = null!;
}
