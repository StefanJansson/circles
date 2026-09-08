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

    // Task 7c — hierarchy: a task can be a sub-task of another task in the same
    // circle. Null for top-level tasks.
    public Guid? ParentTaskId { get; set; }

    // Task 7c — assignment: the person responsible for the task. Null when the
    // task is unassigned. The assignee is a Person (may or may not have a login).
    public Guid? AssignedToPersonId { get; set; }

    // Navigation
    public Circle? Circle { get; set; }
    public Person? CreatedBy { get; set; }
    public Person? AssignedTo { get; set; }
    public CirclesTask? ParentTask { get; set; }
    public ICollection<CirclesTask> SubTasks { get; set; } = new List<CirclesTask>();
}
