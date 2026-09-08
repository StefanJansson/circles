using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Circles.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.CircleId)
            .HasColumnName("circle_id")
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StartsAt)
            .HasColumnName("starts_at")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .HasColumnName("ends_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.Location)
            .HasColumnName("location")
            .HasMaxLength(400);

        builder.Property(e => e.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(200);

        builder.Property(e => e.ExternalSource)
            .HasColumnName("external_source")
            .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Circle)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.CircleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CircleId);
        builder.HasIndex(e => new { e.CircleId, e.ExternalId });
        builder.HasIndex(e => e.StartsAt);
    }
}
