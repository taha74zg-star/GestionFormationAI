namespace AIFormationPlatform.Core.Entities;

public class ReponseQuiz
{
    public int Id { get; set; }
    public int QuestionQuizId { get; set; }
    public required string Texte { get; set; }
    public bool EstCorrecte { get; set; }

    public QuestionQuiz Question { get; set; } = null!;
}
