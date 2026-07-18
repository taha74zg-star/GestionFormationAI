using AIFormationPlatform.Core.Enums;

namespace AIFormationPlatform.Core.Entities;

public class Inscription
{
    public int Id { get; set; }
    public required string ApprenantId { get; set; }
    public int FormationId { get; set; }
    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
    public InscriptionStatut Statut { get; set; } = InscriptionStatut.EnCours;

    public Formation Formation { get; set; } = null!;
}
