namespace Circles.Domain.Entities;

/// <summary>
/// Represents an external iCal/ICS calendar feed that a Circle subscribes to.
/// Generic — works with any standard iCal source (laget.se, Google Calendar, etc.).
/// </summary>
public class CalendarSubscription
{
    public Guid Id { get; set; }
    public Guid CircleId { get; set; }
    
    /// <summary>
    /// Display name for this subscription (e.g., "Laget.se P2011", "Google Kalender").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL to the calendar feed (iCal/ICS format, RFC 5545).
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description or notes about this subscription.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this subscription is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When this subscription was last successfully synced.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Circle Circle { get; set; } = null!;
}
