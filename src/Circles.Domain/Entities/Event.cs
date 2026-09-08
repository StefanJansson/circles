using Circles.Domain.Enums;

namespace Circles.Domain.Entities;

/// <summary>
/// A calendar event (training, match, other activity) belonging to a circle.
///
/// Events are not authored inside Circles — they are imported/synced from an
/// external calendar feed (e.g. a team's laget.se subscription). The
/// <see cref="ExternalId"/> (the feed's UID) together with <see cref="CircleId"/>
/// is used for idempotent upserts so re-syncing never creates duplicates.
/// </summary>
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CircleId { get; set; }
    public Circle? Circle { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public EventType Type { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public string? Location { get; set; }

    // Stable identifier from the external feed (ICS UID). Null for locally
    // seeded/demo events. Used with CircleId for idempotent upserts.
    public string? ExternalId { get; set; }

    // Where the event came from, e.g. "laget.se" or "seed".
    public string? ExternalSource { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
