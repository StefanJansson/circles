using Circles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Circles.Infrastructure.Persistence;

public class CirclesDbContext : DbContext
{
    public CirclesDbContext(DbContextOptions<CirclesDbContext> options) : base(options) { }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Circle> Circles => Set<Circle>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<CirclesTask> Tasks => Set<CirclesTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CirclesDbContext).Assembly);
    }
}
