using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class AIMessageConfiguration : IEntityTypeConfiguration<AIMessage>
{
    public void Configure(EntityTypeBuilder<AIMessage> builder)
    {
        builder.HasKey(am => am.Id);
        builder.HasIndex(am => am.ConversationId);
        builder.HasOne(am => am.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(am => am.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
