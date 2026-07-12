using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.Quizzes.ViewModels;

public class QuizFormViewModel
{
    public int Id { get; set; }

    public int FormationId { get; set; }

    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int TimeLimitMinutes { get; set; }

    public List<QuestionFormViewModel> Questions { get; set; } = new();
}

public class QuestionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le texte de la question est requis")]
    public string Text { get; set; } = string.Empty;

    public int Points { get; set; } = 1;

    public List<AnswerChoiceFormViewModel> AnswerChoices { get; set; } = new();
}

public class AnswerChoiceFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le texte du choix est requis")]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}
