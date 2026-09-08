using Circles.Domain.Enums;

namespace Circles.Web.Shared;

/// <summary>
/// Swedish display labels for backend enum values. Kept in one place so every
/// screen shows the same wording (ported from the previous frontend).
/// </summary>
public static class Labels
{
    public static string Circle(CircleType type) => type switch
    {
        CircleType.Team => "Lag",
        CircleType.Board => "Styrelse",
        CircleType.Officials => "Funktionärer",
        CircleType.General => "Allmän",
        _ => type.ToString(),
    };

    public static string Role(MembershipRole role) => role switch
    {
        MembershipRole.Player => "Spelare",
        MembershipRole.Guardian => "Vårdnadshavare",
        MembershipRole.Coach => "Tränare",
        MembershipRole.Leader => "Ledare",
        MembershipRole.Administrator => "Administratör",
        MembershipRole.Member => "Medlem",
        _ => role.ToString(),
    };

    public static string Module(ModuleType module) => module switch
    {
        ModuleType.Announcements => "Meddelanden",
        ModuleType.Discussions => "Diskussioner",
        ModuleType.Polls => "Omröstningar",
        ModuleType.Tasks => "Uppgifter",
        ModuleType.Events => "Händelser",
        ModuleType.Attendance => "Närvaro",
        _ => module.ToString(),
    };

    /// <summary>Direct vs derived (guardian) access badge text.</summary>
    public static string Access(string accessKind) => accessKind switch
    {
        "Direct" => "Direkt",
        "Derived" => "Härledd",
        _ => accessKind,
    };

    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
