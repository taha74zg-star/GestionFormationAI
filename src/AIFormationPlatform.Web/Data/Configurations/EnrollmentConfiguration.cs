using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.FormationId }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Formation)
            .WithMany(f => f.Enrollments)
            .HasForeignKey(e => e.FormationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
