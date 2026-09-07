using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Circles.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.CircleId)
            .HasColumnName("circle_id")
            .IsRequired();

        builder.Property(a => a.CreatedByPersonId)
            .HasColumnName("created_by_person_id")
            .IsRequired();

        builder.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Body)
            .HasColumnName("body")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(a => a.Circle)
            .WithMany(c => c.Announcements)
            .HasForeignKey(a => a.CircleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.CreatedByPerson)
            .WithMany(p => p.AnnouncementsCreated)
            .HasForeignKey(a => a.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
