using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Circles.Infrastructure.Persistence.Configurations;

public class OrganizationModuleConfiguration : IEntityTypeConfiguration<OrganizationModule>
{
    public void Configure(EntityTypeBuilder<OrganizationModule> builder)
    {
        builder.ToTable("organization_modules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(m => m.Module)
            .HasColumnName("module")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(m => m.Organization)
            .WithMany(o => o.Modules)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.OrganizationId, m.Module }).IsUnique();
    }
}
