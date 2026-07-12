using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(qa => qa.Id);
        builder.HasIndex(qa => qa.UserId);
        builder.HasOne(qa => qa.User)
            .WithMany(u => u.QuizAttempts)
            .HasForeignKey(qa => qa.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(qa => qa.Quiz)
            .WithMany(q => q.QuizAttempts)
            .HasForeignKey(qa => qa.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
