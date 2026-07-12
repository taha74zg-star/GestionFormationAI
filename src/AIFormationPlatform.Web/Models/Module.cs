using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class Module
{
    public int Id { get; set; }

    public int FormationId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Formation Formation { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
