using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public int Points { get; set; } = 1;

    public Quiz Quiz { get; set; } = null!;
    public ICollection<AnswerChoice> AnswerChoices { get; set; } = new List<AnswerChoice>();
}
