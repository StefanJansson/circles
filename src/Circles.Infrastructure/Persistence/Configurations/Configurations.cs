using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Circles.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("persons");
        b.HasKey(p => p.Id);
        b.Property(p => p.FirstName).HasMaxLength(200).IsRequired();
        b.Property(p => p.LastName).HasMaxLength(200).IsRequired();

        // A Person may have zero or one UserAccount. The FK lives on UserAccount.
        b.HasOne(p => p.UserAccount)
            .WithOne(u => u.Person)
            .HasForeignKey<UserAccount>(u => u.PersonId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> b)
    {
        b.ToTable("user_accounts");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasMaxLength(320).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).IsRequired();
    }
}

public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> b)
    {
        b.ToTable("relationships");
        b.HasKey(r => r.Id);
        b.Property(r => r.Type).HasConversion<string>().HasMaxLength(50);

        b.HasOne(r => r.FromPerson)
            .WithMany(p => p.OutgoingRelationships)
            .HasForeignKey(r => r.FromPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(r => r.ToPerson)
            .WithMany(p => p.IncomingRelationships)
            .HasForeignKey(r => r.ToPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(r => new { r.FromPersonId, r.Type });
        b.HasIndex(r => new { r.ToPersonId, r.Type });
    }
}

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("organizations");
        b.HasKey(o => o.Id);
        b.Property(o => o.Name).HasMaxLength(300).IsRequired();
        b.Property(o => o.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(o => o.Slug).IsUnique();
    }
}

public class CircleConfiguration : IEntityTypeConfiguration<Circle>
{
    public void Configure(EntityTypeBuilder<Circle> b)
    {
        b.ToTable("circles");
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).HasMaxLength(300).IsRequired();
        b.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        b.Property(c => c.Type).HasConversion<string>().HasMaxLength(50);
        b.Property(c => c.LagetSeCalendarUrl).HasMaxLength(500);

        b.HasOne(c => c.Organization)
            .WithMany(o => o.Circles)
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(c => c.ParentCircle)
            .WithMany(c => c.ChildCircles)
            .HasForeignKey(c => c.ParentCircleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(c => new { c.OrganizationId, c.Slug }).IsUnique();
    }
}

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> b)
    {
        b.ToTable("memberships");
        b.HasKey(m => m.Id);
        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(50);

        b.HasOne(m => m.Person)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(m => m.Circle)
            .WithMany(c => c.Memberships)
            .HasForeignKey(m => m.CircleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(m => new { m.CircleId, m.ValidUntil });
        b.HasIndex(m => new { m.PersonId, m.ValidUntil });
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(rp => rp.Id);
        b.Property(rp => rp.Role).HasConversion<string>().HasMaxLength(50);
        b.Property(rp => rp.Permission).HasConversion<string>().HasMaxLength(50);
        b.HasIndex(rp => new { rp.Role, rp.Permission }).IsUnique();
    }
}

public class MagicLinkTokenConfiguration : IEntityTypeConfiguration<MagicLinkToken>
{
    public void Configure(EntityTypeBuilder<MagicLinkToken> b)
    {
        b.ToTable("magic_link_tokens");
        b.HasKey(t => t.Id);
        b.Property(t => t.Token).HasMaxLength(128).IsRequired();
        b.HasIndex(t => t.Token).IsUnique();

        b.HasOne(t => t.UserAccount)
            .WithMany()
            .HasForeignKey(t => t.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.UserAccountId);
    }
}

public class DiscussionConfiguration : IEntityTypeConfiguration<Discussion>
{
    public void Configure(EntityTypeBuilder<Discussion> b)
    {
        b.ToTable("discussions");
        b.HasKey(d => d.Id);
        b.Property(d => d.Title).HasMaxLength(500).IsRequired();
        
        b.HasOne(d => d.Circle)
            .WithMany(c => c.Discussions)
            .HasForeignKey(d => d.CircleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasOne(d => d.OriginalPoster)
            .WithMany()
            .HasForeignKey(d => d.OriginalPosterPersonId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasIndex(d => d.CircleId);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("posts");
        b.HasKey(p => p.Id);
        b.Property(p => p.Content).IsRequired();
        
        b.HasOne(p => p.Discussion)
            .WithMany(d => d.Posts)
            .HasForeignKey(p => p.DiscussionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        b.HasOne(p => p.Person)
            .WithMany()
            .HasForeignKey(p => p.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasIndex(p => new { p.DiscussionId, p.CreatedAt });
    }
}

public class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> b)
    {
        b.ToTable("polls");
        b.HasKey(p => p.Id);
        b.Property(p => p.Title).HasMaxLength(500).IsRequired();
        
        b.HasOne(p => p.Circle)
            .WithMany(c => c.Polls)
            .HasForeignKey(p => p.CircleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasIndex(p => p.CircleId);
    }
}

public class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> b)
    {
        b.ToTable("poll_options");
        b.HasKey(po => po.Id);
        b.Property(po => po.Text).HasMaxLength(500).IsRequired();
        
        b.HasOne(po => po.Poll)
            .WithMany(p => p.Options)
            .HasForeignKey(po => po.PollId)
            .OnDelete(DeleteBehavior.Cascade);
        
        b.HasIndex(po => new { po.PollId, po.Order });
    }
}

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> b)
    {
        b.ToTable("votes");
        b.HasKey(v => v.Id);
        
        b.HasOne(v => v.Option)
            .WithMany(po => po.Votes)
            .HasForeignKey(v => v.PollOptionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        b.HasOne(v => v.Person)
            .WithMany()
            .HasForeignKey(v => v.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Ensure one vote per person per poll
        b.HasIndex(v => new { v.PollOptionId, v.PersonId }).IsUnique();
    }
}

public class CirclesTaskConfiguration : IEntityTypeConfiguration<CirclesTask>
{
    public void Configure(EntityTypeBuilder<CirclesTask> b)
    {
        b.ToTable("circles_tasks");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).HasMaxLength(500).IsRequired();
        b.Property(t => t.Description).IsRequired();
        
        b.HasOne(t => t.Circle)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CircleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.HasIndex(t => t.CircleId);
        b.HasIndex(t => new { t.CircleId, t.CompletedAt });
    }
}
