using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIFormationPlatform.Core.Entities;

public class Formation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Titre { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    // Categorie stockée comme texte pour simplifier le catalogue (nom de catégorie)
    [Required]
    [MaxLength(150)]
    public string Categorie { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Niveau { get; set; } = "Débutant";

    [Column("DureeEnMinutes")]
    public int DureeEnMinutes { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public string Contenu { get; set; } = string.Empty;

    public string? Objectifs { get; set; }

    public string? Prerequis { get; set; }

    public bool EstPubliee { get; set; } = false;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime? DateModification { get; set; }

    // Relations preserved where applicable (modules, inscriptions)
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
}
