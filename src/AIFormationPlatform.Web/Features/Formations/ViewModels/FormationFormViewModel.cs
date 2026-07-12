using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.Formations.ViewModels;

public class FormationFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
}
