namespace Circles.Domain.Entities;

public class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CircleId { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Optional link to a calendar event this announcement concerns
    // (e.g. "Info inför matchen på lördag"). Nullable — most announcements
    // are not tied to a specific event.
    public Guid? EventId { get; set; }

    // Navigation properties
    public Circle? Circle { get; set; }
    public Person? CreatedByPerson { get; set; }
    public Event? Event { get; set; }
}
