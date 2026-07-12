namespace AIFormationPlatform.Web.Features.Formations.ViewModels;

public class FormationDetailViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ModuleSummaryViewModel> Modules { get; set; } = new();
}

public class ModuleSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int LessonCount { get; set; }
}
