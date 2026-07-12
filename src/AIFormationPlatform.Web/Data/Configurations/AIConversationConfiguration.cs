using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class AIConversationConfiguration : IEntityTypeConfiguration<AIConversation>
{
    public void Configure(EntityTypeBuilder<AIConversation> builder)
    {
        builder.HasKey(ac => ac.Id);
        builder.HasIndex(ac => ac.UserId);
        builder.HasOne(ac => ac.User)
            .WithMany(u => u.AIConversations)
            .HasForeignKey(ac => ac.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ac => ac.Lesson)
            .WithMany(l => l.AIConversations)
            .HasForeignKey(ac => ac.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
