using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.HasKey(lp => lp.Id);
        builder.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();
        builder.HasOne(lp => lp.User)
            .WithMany(u => u.LessonProgresses)
            .HasForeignKey(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(lp => lp.Lesson)
            .WithMany(l => l.LessonProgresses)
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
