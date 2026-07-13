using AIFormationPlatform.Core.Enums;

namespace AIFormationPlatform.Core.Entities;

public class ContenuCours
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public ContenuType Type { get; set; }
    public string? Contenu { get; set; }
    public string? UrlFichier { get; set; }
    public int Ordre { get; set; }

    public Module Module { get; set; } = null!;
}
