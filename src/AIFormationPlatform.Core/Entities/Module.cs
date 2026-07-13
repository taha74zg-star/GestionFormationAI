namespace AIFormationPlatform.Core.Entities;

public class Module
{
    public int Id { get; set; }
    public int FormationId { get; set; }
    public required string Titre { get; set; }
    public string? Description { get; set; }
    public int Ordre { get; set; }
    public string? ScriptAvatar { get; set; }

    public Formation Formation { get; set; } = null!;
    public ICollection<ContenuCours> Contenus { get; set; } = [];
    public Exercice? Exercice { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<Progression> Progressions { get; set; } = [];
}
