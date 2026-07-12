namespace AIFormationPlatform.Web.Features.Enrollments.ViewModels;

public class EnrollmentViewModel
{
    public int Id { get; set; }
    public int FormationId { get; set; }
    public string FormationTitle { get; set; } = string.Empty;
    public string FormationDescription { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public double ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
}
