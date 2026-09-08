using Circles.Domain.Enums;

namespace Circles.Domain.Entities;

/// <summary>
/// A scoped space within an organization — a team, board, group of officials, etc.
///
/// Circles can be nested (<see cref="ParentCircleId"/>) to form a hierarchy, e.g.
/// the root club circle contains the individual teams. Circles are owned by the
/// organization and persist independently of the people currently in them.
/// </summary>
public class Circle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    // Null for the organization's root circle; set for nested circles.
    public Guid? ParentCircleId { get; set; }
    public Circle? ParentCircle { get; set; }

    public ICollection<Circle> ChildCircles { get; set; } = new List<Circle>();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    // Optional free-text description shown in the admin panel.
    public string? Description { get; set; }

    public CircleType Type { get; set; }

    // Archived circles are hidden from active listings but never deleted, so the
    // circle (and all its history) persists. Archiving is reversible.
    public bool IsArchived { get; set; }

    // Optional external calendar subscription (ICS/iCal URL) for this circle,
    // e.g. a team's laget.se feed. When set, events can be synced in from it.
    public string? LagetSeCalendarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public ICollection<CirclesTask> Tasks { get; set; } = new List<CirclesTask>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
