using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.Modules.ViewModels;

public class ModuleFormViewModel
{
    public int Id { get; set; }

    public int FormationId { get; set; }

    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
