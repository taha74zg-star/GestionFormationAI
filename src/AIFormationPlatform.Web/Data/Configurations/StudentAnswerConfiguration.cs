using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.HasKey(sa => sa.Id);
        builder.HasIndex(sa => sa.QuizAttemptId);
        builder.HasOne(sa => sa.QuizAttempt)
            .WithMany(qa => qa.StudentAnswers)
            .HasForeignKey(sa => sa.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(sa => sa.Question)
            .WithMany()
            .HasForeignKey(sa => sa.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(sa => sa.AnswerChoice)
            .WithMany()
            .HasForeignKey(sa => sa.AnswerChoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
