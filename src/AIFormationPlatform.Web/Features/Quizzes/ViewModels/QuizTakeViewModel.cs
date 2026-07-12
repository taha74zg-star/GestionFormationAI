namespace AIFormationPlatform.Web.Features.Quizzes.ViewModels;

public class QuizTakeViewModel
{
    public int Id { get; set; }
    public int FormationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; }
    public List<QuestionTakeViewModel> Questions { get; set; } = new();
}

public class QuestionTakeViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public List<AnswerChoiceTakeViewModel> AnswerChoices { get; set; } = new();
}

public class AnswerChoiceTakeViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
