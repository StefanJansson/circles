namespace Circles.Domain.Enums;

/// <summary>
/// The type of an explicit, time-based relationship between two people.
/// Relationships are first-class domain objects, not implicit links.
/// </summary>
public enum RelationshipType
{
    GuardianOf = 0,
    ChildOf = 1,
    LeaderOf = 2,
    ContactPerson = 3
}

/// <summary>
/// The kind of a circle. A circle is a scoped space owned by an organization.
/// </summary>
public enum CircleType
{
    Team = 0,
    Board = 1,
    Officials = 2,
    General = 3
}

/// <summary>
/// The role a person holds through a (time-based) membership in a circle.
/// </summary>
public enum MembershipRole
{
    Player = 0,
    Guardian = 1,
    Coach = 2,
    Leader = 3,
    Administrator = 4,
    Member = 5
}

/// <summary>
/// A concrete capability that can be granted within a circle.
/// </summary>
public enum PermissionType
{
    ReadPosts = 0,
    CreateDiscussion = 1,
    Comment = 2,
    CreatePoll = 3,
    Vote = 4,
    CreateTask = 5,
    AdministerMembers = 6,
    PublishAnnouncements = 7,
    ViewMemberList = 8,
    ViewHistoricalInfo = 9
}

/// <summary>
/// A feature module that can be switched on or off per organization.
/// Modules are opt-in: each club (organization) decides which features it uses,
/// so sport-specific capabilities (e.g. events, attendance) are never forced on
/// every organization.
/// </summary>
public enum ModuleType
{
    Announcements = 0,
    Discussions = 1,
    Polls = 2,
    Tasks = 3,
    Events = 4,
    Attendance = 5
}

/// <summary>
/// The kind of a calendar event. Mapped from the external calendar feed's
/// category (e.g. laget.se: "Match" → Match, "Träning" → Training, other → Other).
/// </summary>
public enum EventType
{
    Match = 0,
    Training = 1,
    Other = 2
}
