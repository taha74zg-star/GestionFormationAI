namespace AIFormationPlatform.Core.Entities;

public class Formateur
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Nom { get; set; }
    public required string Prenom { get; set; }
    public string? Bio { get; set; }
    public string? Specialites { get; set; }
    public string? PhotoUrl { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public ICollection<Formation> Formations { get; set; } = [];
}
