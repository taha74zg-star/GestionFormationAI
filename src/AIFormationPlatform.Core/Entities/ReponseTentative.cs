namespace AIFormationPlatform.Core.Entities;

public class ReponseTentative
{
    public int Id { get; set; }
    public int TentativeQuizId { get; set; }
    public int QuestionQuizId { get; set; }
    public int? ReponseQuizId { get; set; }
    public string? ReponseTexte { get; set; }
    public bool EstCorrecte { get; set; }

    public TentativeQuiz Tentative { get; set; } = null!;
    public QuestionQuiz Question { get; set; } = null!;
    public ReponseQuiz? ReponseSelectionnee { get; set; }
}
