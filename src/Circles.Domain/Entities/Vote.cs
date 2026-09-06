namespace Circles.Domain.Entities;

/// <summary>
/// A vote is a person's choice in a poll.
/// </summary>
public class Vote
{
    public Guid Id { get; set; }
    public Guid PollOptionId { get; set; }
    public Guid PersonId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public PollOption? Option { get; set; }
    public Person? Person { get; set; }
}
