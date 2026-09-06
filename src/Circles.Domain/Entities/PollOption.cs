namespace Circles.Domain.Entities;

/// <summary>
/// An option in a poll.
/// </summary>
public class PollOption
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    
    // Navigation
    public Poll? Poll { get; set; }
    public ICollection<Vote> Votes { get; set; } = [];
}
