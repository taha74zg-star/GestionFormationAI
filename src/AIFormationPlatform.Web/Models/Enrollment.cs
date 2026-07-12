using System.ComponentModel.DataAnnotations;

namespace AIFormationPlatform.Web.Models;

public class Enrollment
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int FormationId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Formation Formation { get; set; } = null!;
}
