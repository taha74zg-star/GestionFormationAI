using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.HasIndex(m => m.FormationId);
        builder.HasOne(m => m.Formation)
            .WithMany(f => f.Modules)
            .HasForeignKey(m => m.FormationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
