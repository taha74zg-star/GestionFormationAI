using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(q => q.FormationId);
        builder.HasOne(q => q.Formation)
            .WithMany(f => f.Quizzes)
            .HasForeignKey(q => q.FormationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
