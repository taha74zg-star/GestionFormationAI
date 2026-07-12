namespace AIFormationPlatform.Web.Features.Formations.ViewModels;

public class FormationListViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int ModuleCount { get; set; }
    public int EnrollmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
