namespace Circles.Domain.Entities;

/// <summary>
/// Represents a human being.
///
/// A Person is deliberately independent of authentication. A Person may exist
/// WITHOUT a <see cref="UserAccount"/> — for example, a child who is registered
/// as a player long before they ever have a login. Identity (who someone is)
/// is separated from authentication (how someone signs in) and from membership
/// (what someone belongs to).
/// </summary>
public class Person
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional link to the account used to sign in as this person.
    // Nullable: a person can exist with no way to log in.
    public UserAccount? UserAccount { get; set; }

    // Memberships this person holds (current and historical).
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    // Relationships where this person is the source (e.g. Johan GuardianOf Alexander).
    public ICollection<Relationship> OutgoingRelationships { get; set; } = new List<Relationship>();

    // Relationships where this person is the target (e.g. Alexander is child in Johan GuardianOf Alexander).
    public ICollection<Relationship> IncomingRelationships { get; set; } = new List<Relationship>();

    public ICollection<Announcement> AnnouncementsCreated { get; set; } = new List<Announcement>();

    public string FullName => $"{FirstName} {LastName}";
}
