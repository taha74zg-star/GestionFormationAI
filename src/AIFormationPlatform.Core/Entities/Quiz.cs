namespace AIFormationPlatform.Core.Entities;

public class Quiz
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public required string Titre { get; set; }
    public int ScoreMinimum { get; set; } = 50;
    public int TentativesMax { get; set; } = 3;

    public Module Module { get; set; } = null!;
    public ICollection<QuestionQuiz> Questions { get; set; } = [];
    public ICollection<TentativeQuiz> Tentatives { get; set; } = [];
}
