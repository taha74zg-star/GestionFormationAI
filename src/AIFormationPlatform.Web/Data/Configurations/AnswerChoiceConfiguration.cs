using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class AnswerChoiceConfiguration : IEntityTypeConfiguration<AnswerChoice>
{
    public void Configure(EntityTypeBuilder<AnswerChoice> builder)
    {
        builder.HasKey(ac => ac.Id);
        builder.Property(ac => ac.Text).IsRequired().HasMaxLength(500);
        builder.HasIndex(ac => ac.QuestionId);
        builder.HasOne(ac => ac.Question)
            .WithMany(q => q.AnswerChoices)
            .HasForeignKey(ac => ac.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
