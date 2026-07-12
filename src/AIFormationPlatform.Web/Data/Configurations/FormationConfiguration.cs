using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIFormationPlatform.Web.Data.Configurations;

public class FormationConfiguration : IEntityTypeConfiguration<Formation>
{
    public void Configure(EntityTypeBuilder<Formation> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Title).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Description).HasMaxLength(2000);
        builder.HasIndex(f => f.IsPublished);
    }
}
