namespace Circles.Domain.Entities;

/// <summary>
/// A task is an item to be completed within a circle.
/// Named CirclesTask to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
public class CirclesTask
{
    public Guid Id { get; set; }
    public Guid CircleId { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation
    public Circle? Circle { get; set; }
    public Person? CreatedBy { get; set; }
}
