using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Infrastructure.Seeding;

/// <summary>
/// Seeds the database with realistic Swedish demo data for the fictional club
/// "Uppsala IK", plus the role → permission mapping. Deterministic GUIDs are used
/// so the seed is idempotent and stable across runs.
/// </summary>
public static class DataSeeder
{
    private static Guid Id(string key) =>
        new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key)));

    // Fixed reference "now" so ValidFrom dates are stable and clearly in the past.
    private static readonly DateTime Now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Shared password for every demo account so the prototype is easy to log in
    // to. NOT a real credential policy — real accounts set their own password
    // during onboarding. Hashed with BCrypt at seed time.
    public const string DemoPassword = "Cirkles123!";
    private static readonly string DemoPasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

    public static async Task SeedAsync(CirclesDbContext db)
    {
        await SeedRolePermissionsAsync(db);
        await SeedUppsalaIkAsync(db);
        await SeedContentAsync(db);
    }

    private static async Task SeedRolePermissionsAsync(CirclesDbContext db)
    {
        if (await db.RolePermissions.AnyAsync()) return;

        foreach (var (role, permissions) in RolePermissionMap.Map)
        {
            foreach (var permission in permissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Id($"rp:{role}:{permission}"),
                    Role = role,
                    Permission = permission
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedUppsalaIkAsync(CirclesDbContext db)
    {
        if (await db.Organizations.AnyAsync()) return;

        // ---- Organization -------------------------------------------------
        var org = new Organization
        {
            Id = Id("org:uppsala-ik"),
            Name = "Uppsala IK",
            Slug = "uppsala-ik",
            CreatedAt = Now
        };
        db.Organizations.Add(org);

        // ---- Circles ------------------------------------------------------
        var root = new Circle
        {
            Id = Id("circle:uppsala-ik"),
            OrganizationId = org.Id,
            ParentCircleId = null,
            Name = "Uppsala IK",
            Slug = "uppsala-ik",
            Type = CircleType.General,
            CreatedAt = Now
        };
        var p2016 = new Circle
        {
            Id = Id("circle:p2016"),
            OrganizationId = org.Id,
            ParentCircleId = root.Id,
            Name = "P2016",
            Slug = "p2016",
            Type = CircleType.Team,
            CreatedAt = Now
        };
        var p2014 = new Circle
        {
            Id = Id("circle:p2014"),
            OrganizationId = org.Id,
            ParentCircleId = root.Id,
            Name = "P2014",
            Slug = "p2014",
            Type = CircleType.Team,
            CreatedAt = Now
        };
        var f2016 = new Circle
        {
            Id = Id("circle:f2016"),
            OrganizationId = org.Id,
            ParentCircleId = root.Id,
            Name = "F2016",
            Slug = "f2016",
            Type = CircleType.Team,
            CreatedAt = Now
        };
        var board = new Circle
        {
            Id = Id("circle:styrelsen"),
            OrganizationId = org.Id,
            ParentCircleId = root.Id,
            Name = "Styrelsen",
            Slug = "styrelsen",
            Type = CircleType.Board,
            CreatedAt = Now
        };
        var officials = new Circle
        {
            Id = Id("circle:funktionarer"),
            OrganizationId = org.Id,
            ParentCircleId = root.Id,
            Name = "Funktionärer",
            Slug = "funktionarer",
            Type = CircleType.General,
            CreatedAt = Now
        };
        db.Circles.AddRange(root, p2016, p2014, f2016, board, officials);

        // ---- People (some with accounts, some without) --------------------
        // Johan Andersson — has a UserAccount, guardian of Alexander.
        var johan = NewPerson("johan", "Johan", "Andersson");
        // Alexander Andersson — a 10-year-old child with NO UserAccount.
        var alexander = NewPerson("alexander", "Alexander", "Andersson");
        // Lisa Berg — a child with NO UserAccount.
        var lisa = NewPerson("lisa", "Lisa", "Berg");
        // Anna Berg — has a UserAccount, guardian of Lisa.
        var anna = NewPerson("anna", "Anna", "Berg");
        // Erik Svensson — has a UserAccount, coach.
        var erik = NewPerson("erik", "Erik", "Svensson");
        // Maria Lindgren — has a UserAccount, club administrator.
        var maria = NewPerson("maria", "Maria", "Lindgren");
        db.Persons.AddRange(johan, alexander, lisa, anna, erik, maria);

        db.UserAccounts.AddRange(
            NewAccount("johan", "johan@example.com", johan.Id),
            NewAccount("anna", "anna@example.com", anna.Id),
            NewAccount("erik", "erik@example.com", erik.Id),
            NewAccount("maria", "maria@example.com", maria.Id)
        );
        // NOTE: Alexander and Lisa deliberately have NO UserAccount.

        // ---- Relationships (explicit, time-based) -------------------------
        db.Relationships.AddRange(
            NewRelationship("johan-guardian-alexander", johan.Id, alexander.Id, RelationshipType.GuardianOf),
            NewRelationship("anna-guardian-lisa", anna.Id, lisa.Id, RelationshipType.GuardianOf)
        );

        // ---- Memberships (time-based, never deleted) ----------------------
        db.Memberships.AddRange(
            NewMembership("alexander-p2016", alexander.Id, p2016.Id, MembershipRole.Player),
            NewMembership("lisa-f2016", lisa.Id, f2016.Id, MembershipRole.Player),
            NewMembership("erik-p2016", erik.Id, p2016.Id, MembershipRole.Coach),
            NewMembership("maria-root", maria.Id, root.Id, MembershipRole.Administrator),
            NewMembership("johan-officials", johan.Id, officials.Id, MembershipRole.Member)
        );
        // NOTE: Johan has NO direct membership in P2016. His access to P2016 is
        // DERIVED from being guardian of Alexander, who plays in P2016.

        await db.SaveChangesAsync();
    }

    private static Person NewPerson(string key, string first, string last) => new()
    {
        Id = Id($"person:{key}"),
        FirstName = first,
        LastName = last,
        CreatedAt = Now
    };

    private static UserAccount NewAccount(string key, string email, Guid personId) => new()
    {
        Id = Id($"account:{key}"),
        Email = email,
        PasswordHash = DemoPasswordHash,
        PersonId = personId,
        CreatedAt = Now
    };

    private static Relationship NewRelationship(string key, Guid from, Guid to, RelationshipType type) => new()
    {
        Id = Id($"rel:{key}"),
        FromPersonId = from,
        ToPersonId = to,
        Type = type,
        ValidFrom = Now.AddYears(-2),
        ValidUntil = null
    };

    private static Membership NewMembership(string key, Guid personId, Guid circleId, MembershipRole role) => new()
    {
        Id = Id($"mem:{key}"),
        PersonId = personId,
        CircleId = circleId,
        Role = role,
        ValidFrom = Now.AddYears(-1),
        ValidUntil = null
    };

    /// <summary>
    /// Seeds one demo Discussion, one Poll, and one Task so the UI has content
    /// to display immediately after a fresh install. Skipped if already present.
    /// </summary>
    private static async Task SeedContentAsync(CirclesDbContext db)
    {
        if (await db.Discussions.AnyAsync()) return;

        var p2016Id    = Id("circle:p2016");
        var officialId = Id("circle:funktionarer");
        var erikId     = Id("person:erik");
        var johanId    = Id("person:johan");
        var mariaId    = Id("person:maria");

        // ── Demo Discussion in P2016 ────────────────────────────────────────
        var discussion = new Discussion
        {
            Id          = Id("discussion:match-tider"),
            CircleId    = p2016Id,
            OriginalPosterPersonId = erikId,
            Title       = "Matchider för kommande säsong",
            CreatedAt   = Now.AddDays(10)
        };
        var post1 = new Post
        {
            Id           = Id("post:match-tider-1"),
            DiscussionId = discussion.Id,
            PersonId     = erikId,
            Content      = "Hej alla! Har ni möjlighet att spela på lördagar framöver? Jag behöver boka tider.",
            CreatedAt    = Now.AddDays(10)
        };
        var post2 = new Post
        {
            Id           = Id("post:match-tider-2"),
            DiscussionId = discussion.Id,
            PersonId     = mariaId,
            Content      = "Lördagar fungerar bra för oss. Tack för att du kollar!",
            CreatedAt    = Now.AddDays(10).AddHours(1)
        };
        db.Discussions.Add(discussion);
        db.Posts.AddRange(post1, post2);

        // ── Demo Poll in Funktionärer ───────────────────────────────────────
        var poll = new Poll
        {
            Id        = Id("poll:traning-dag"),
            CircleId  = officialId,
            Title     = "Vilken dag passar träning bäst?",
            CreatedAt = Now.AddDays(5)
        };
        var optTue = new PollOption { Id = Id("pollopt:tisdag"),  PollId = poll.Id, Text = "Tisdag",  Order = 0 };
        var optThu = new PollOption { Id = Id("pollopt:torsdag"), PollId = poll.Id, Text = "Torsdag", Order = 1 };
        var optSat = new PollOption { Id = Id("pollopt:lordag"),  PollId = poll.Id, Text = "Lördag",  Order = 2 };
        poll.Options.Add(optTue);
        poll.Options.Add(optThu);
        poll.Options.Add(optSat);

        // Johan röstar på Tisdag
        var vote1 = new Vote { Id = Id("vote:johan-tisdag"), PollOptionId = optTue.Id, PersonId = johanId, CreatedAt = Now.AddDays(6) };
        // Maria röstar på Torsdag
        var vote2 = new Vote { Id = Id("vote:maria-torsdag"), PollOptionId = optThu.Id, PersonId = mariaId, CreatedAt = Now.AddDays(6) };

        db.Polls.Add(poll);
        db.Votes.AddRange(vote1, vote2);

        // ── Demo Task in P2016 ──────────────────────────────────────────────
        var circlesTask = new CirclesTask
        {
            Id                = Id("task:boka-plan"),
            CircleId          = p2016Id,
            CreatedByPersonId = erikId,
            Title             = "Boka plan inför säsongsstart",
            Description       = "Kontakta idrottshallen och boka tider för träning v.15–v.20.",
            DueDate           = Now.AddDays(30),
            CreatedAt         = Now.AddDays(2)
        };
        db.Tasks.Add(circlesTask);


        // ── Demo Announcements in P2016 ────────────────────────────────────
        var ann1 = new Announcement
        {
            Id                = Id("ann:sakermote"),
            CircleId          = p2016Id,
            CreatedByPersonId = erikId,
            Title             = "Säkerhetsmöte inför säsongsstart",
            Body              = "Hej alla i P2016! Vi har ett obligatoriskt säkerhetsmöte torsdagen den 20 mars kl. 18:00 i klubbstugan. Alla spelare och föräldrar är välkomna.",
            CreatedAt         = Now.AddDays(1)
        };
        var ann2 = new Announcement
        {
            Id                = Id("ann:troja"),
            CircleId          = p2016Id,
            CreatedByPersonId = erikId,
            Title             = "Nya träningströjor har kommit!",
            Body              = "De nya träningströjorna finns nu att hämta hos Lars i omklädningsrummet. Hämta er tröja senast på fredag.",
            CreatedAt         = Now.AddDays(3)
        };
        db.Announcements.AddRange(ann1, ann2);

        await db.SaveChangesAsync();
    }
}
