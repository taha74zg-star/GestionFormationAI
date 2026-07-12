namespace AIFormationPlatform.Web.Features.Quizzes.ViewModels;

public class QuizResultViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public double Percentage { get; set; }
    public DateTime AttemptedAt { get; set; }
    public List<QuestionResultViewModel> Questions { get; set; } = new();
}

public class QuestionResultViewModel
{
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
