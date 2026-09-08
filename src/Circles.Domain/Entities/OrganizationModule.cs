using Circles.Domain.Enums;

namespace Circles.Domain.Entities;

/// <summary>
/// An opt-in feature flag for an organization. Each row records whether a given
/// <see cref="ModuleType"/> is enabled for the organization. Features are opt-in
/// per club, so sport-specific capabilities (events, attendance, …) are only
/// active where the organization has chosen to switch them on.
/// </summary>
public class OrganizationModule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public ModuleType Module { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
