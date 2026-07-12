namespace AIFormationPlatform.Web.Features.Lessons.ViewModels;

public class LessonDetailViewModel
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int FormationId { get; set; }
    public string FormationTitle { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
