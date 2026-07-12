namespace AIFormationPlatform.Web.Features.Enrollments.ViewModels;

public class ProgressViewModel
{
    public int FormationId { get; set; }
    public string FormationTitle { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public List<ModuleProgressViewModel> Modules { get; set; } = new();
}

public class ModuleProgressViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<LessonProgressItemViewModel> Lessons { get; set; } = new();
}

public class LessonProgressItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
