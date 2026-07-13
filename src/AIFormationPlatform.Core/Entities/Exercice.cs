using AIFormationPlatform.Core.Enums;

namespace AIFormationPlatform.Core.Entities;

public class Exercice
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public required string Enonce { get; set; }
    public ExerciceType Type { get; set; }
    public string? Consignes { get; set; }

    public Module Module { get; set; } = null!;
}
