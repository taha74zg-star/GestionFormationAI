namespace AIFormationPlatform.Core.Entities;

public class Categorie
{
    public int Id { get; set; }
    public required string Nom { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int Ordre { get; set; }

    public Categorie? Parent { get; set; }
    public ICollection<Categorie> Enfants { get; set; } = [];
    public ICollection<Formation> Formations { get; set; } = [];
}
