using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Circles.Infrastructure.Persistence.Configurations;

public class CalendarSubscriptionConfiguration : IEntityTypeConfiguration<CalendarSubscription>
{
    public void Configure(EntityTypeBuilder<CalendarSubscription> builder)
    {
        builder.ToTable("calendar_subscriptions");

        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(cs => cs.CircleId)
            .HasColumnName("circle_id")
            .IsRequired();

        builder.Property(cs => cs.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cs => cs.Url)
            .HasColumnName("url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(cs => cs.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(cs => cs.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(cs => cs.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .HasColumnType("datetime2");

        builder.Property(cs => cs.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(cs => cs.Circle)
            .WithMany(c => c.CalendarSubscriptions)
            .HasForeignKey(cs => cs.CircleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cs => cs.CircleId);
        builder.HasIndex(cs => new { cs.CircleId, cs.IsActive });
    }
}
