using AIFormationPlatform.Core.Enums;

namespace AIFormationPlatform.Core.Entities;

public class QuestionQuiz
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public required string Enonce { get; set; }
    public QuestionType Type { get; set; }
    public int Points { get; set; } = 1;
    public int Ordre { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<ReponseQuiz> Reponses { get; set; } = [];
}
