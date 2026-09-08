namespace Circles.Domain.Entities;

/// <summary>
/// An organization (e.g. a sports club) that owns circles.
///
/// The organization owns its circles: circles persist even when the people who
/// once led them leave. A coach departing does not delete the team.
/// </summary>
public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Circle> Circles { get; set; } = new List<Circle>();

    // Opt-in feature modules this organization has enabled.
    public ICollection<OrganizationModule> Modules { get; set; } = new List<OrganizationModule>();
}
