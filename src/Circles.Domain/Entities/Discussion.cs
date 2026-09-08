namespace Circles.Domain.Entities;

/// <summary>
/// A discussion is a threaded conversation within a circle.
/// </summary>
public class Discussion
{
    public Guid Id { get; set; }
    public Guid CircleId { get; set; }
    public Guid OriginalPosterPersonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional link to a calendar event this discussion concerns
    // (e.g. a thread about a specific match). Nullable.
    public Guid? EventId { get; set; }
    
    // Navigation
    public Circle? Circle { get; set; }
    public Person? OriginalPoster { get; set; }
    public Event? Event { get; set; }
    public ICollection<Post> Posts { get; set; } = [];
}
