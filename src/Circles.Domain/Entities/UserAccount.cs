namespace Circles.Domain.Entities;

/// <summary>
/// Represents an authentication identity — i.e. login credentials.
///
/// A UserAccount is NOT a person and NOT a membership. It is only the means by
/// which a human authenticates. It links to a <see cref="Person"/> via the
/// nullable <see cref="PersonId"/> so that, once logged in, the system can
/// resolve the human being and, from there, derive memberships and permissions.
/// </summary>
public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Site administrator: a platform-level identity that can see and manage every
    // organization (e.g. the operator of the whole Circles site). This is NOT a
    // club role — it lives above organizations. Ordinary club admins have an
    // Administrator membership in their organization's root circle instead.
    public bool IsSiteAdmin { get; set; }

    // The person this account authenticates as. Nullable because an account
    // could, in principle, be provisioned before being linked to a person.
    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
