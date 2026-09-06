namespace Circles.Domain.Entities;

/// <summary>
/// A poll is a voting mechanism within a circle.
/// </summary>
public class Poll
{
    public Guid Id { get; set; }
    public Guid CircleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    
    // Navigation
    public Circle? Circle { get; set; }
    public ICollection<PollOption> Options { get; set; } = [];
}
