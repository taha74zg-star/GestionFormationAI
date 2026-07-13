namespace AIFormationPlatform.Core.Entities;

public class TentativeQuiz
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int QuizId { get; set; }
    public int Score { get; set; }
    public int NumeroTentative { get; set; }
    public DateTime DateTentative { get; set; } = DateTime.UtcNow;
    public bool EstReussie { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<ReponseTentative> Reponses { get; set; } = [];
}
