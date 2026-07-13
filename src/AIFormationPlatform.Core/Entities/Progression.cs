namespace AIFormationPlatform.Core.Entities;

public class Progression
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int ModuleId { get; set; }
    public bool EstComplete { get; set; }
    public DateTime? DateCompletion { get; set; }
    public int? ScoreQuiz { get; set; }

    public Module Module { get; set; } = null!;
}
