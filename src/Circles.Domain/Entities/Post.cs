namespace Circles.Domain.Entities;

/// <summary>
/// A post is a message within a discussion.
/// </summary>
public class Post
{
    public Guid Id { get; set; }
    public Guid DiscussionId { get; set; }
    public Guid PersonId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Discussion? Discussion { get; set; }
    public Person? Person { get; set; }
}
