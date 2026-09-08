using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Circles.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

public record EventDto(
    Guid Id,
    Guid CircleId,
    string Title,
    string? Description,
    EventType Type,
    DateTime StartsAt,
    DateTime? EndsAt,
    string? Location,
    string? ExternalSource
);

/// <summary>
/// Reads a circle's calendar events and triggers a re-sync from the circle's
/// external calendar subscription. Events themselves are never authored here —
/// they are imported by <see cref="ICalendarSyncService"/>.
/// </summary>
public class EventService(
    CirclesDbContext db,
    IAuthorizationService authz,
    ICalendarSyncService calendarSync)
{
    private async Task RequireAnyPermissionAsync(
        Guid personId, Guid circleId, params PermissionType[] anyOf)
    {
        var perms = await authz.GetPersonPermissionsInCircleAsync(personId, circleId);
        if (!anyOf.Any(perms.Contains))
            throw new UnauthorizedAccessException(
                $"Saknar behörighet: någon av {string.Join(", ", anyOf)}");
    }

    /// <summary>Upcoming and recent events for the circle, chronological.</summary>
    public async Task<List<EventDto>> GetEventsAsync(Guid personId, Guid circleId)
    {
        await RequireAnyPermissionAsync(personId, circleId, PermissionType.ReadPosts);

        return await db.Events
            .Where(e => e.CircleId == circleId)
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventDto(
                e.Id,
                e.CircleId,
                e.Title,
                e.Description,
                e.Type,
                e.StartsAt,
                e.EndsAt,
                e.Location,
                e.ExternalSource
            ))
            .ToListAsync();
    }

    /// <summary>
    /// Re-imports the circle's events from its laget.se subscription. Requires a
    /// managing permission. Returns the number of events created or updated.
    /// </summary>
    public async Task<int> SyncEventsAsync(Guid personId, Guid circleId)
    {
        await RequireAnyPermissionAsync(
            personId, circleId,
            PermissionType.PublishAnnouncements, PermissionType.AdministerMembers);

        return await calendarSync.SyncCircleAsync(circleId);
    }
}
