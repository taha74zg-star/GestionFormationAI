using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Features.Lessons.ViewModels;

public class LessonFormViewModel
{
    public int Id { get; set; }

    public int ModuleId { get; set; }

    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le contenu est requis")]
    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
