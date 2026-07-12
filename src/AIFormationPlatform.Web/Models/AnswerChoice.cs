using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class AnswerChoice
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public Question Question { get; set; } = null!;
}
