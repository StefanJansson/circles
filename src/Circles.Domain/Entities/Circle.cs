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

    public CircleType Type { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public ICollection<CirclesTask> Tasks { get; set; } = new List<CirclesTask>();
}
