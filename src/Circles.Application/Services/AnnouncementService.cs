using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Circles.Application.Services;

public record AnnouncementDto(
    Guid Id,
    Guid CircleId,
    string CircleName,
    Guid CreatedByPersonId,
    string CreatedByName,
    string Title,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? EventId,
    string? EventTitle
);

public class AnnouncementService(
    CirclesDbContext db,
    IAuthorizationService authz,
    IServiceScopeFactory scopeFactory,
    ILogger<AnnouncementService> logger)
{
    private async Task RequirePermissionAsync(Guid personId, Guid circleId, PermissionType perm)
    {
        var perms = await authz.GetPersonPermissionsInCircleAsync(personId, circleId);
        if (!perms.Contains(perm))
            throw new UnauthorizedAccessException($"Saknar behörighet: {perm}");
    }

    public async Task<List<AnnouncementDto>> GetAnnouncementsAsync(Guid personId, Guid circleId)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.ReadPosts);

        return await db.Announcements
            .Where(a => a.CircleId == circleId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.CircleId,
                a.Circle!.Name,
                a.CreatedByPersonId,
                a.CreatedByPerson!.FirstName + " " + a.CreatedByPerson.LastName,
                a.Title,
                a.Body,
                a.CreatedAt,
                a.UpdatedAt,
                a.EventId,
                a.Event != null ? a.Event.Title : null
            ))
            .ToListAsync();
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(
        Guid personId, Guid circleId, string title, string body, Guid? eventId = null)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.PublishAnnouncements);

        // Only accept an event link that belongs to the same circle.
        Guid? validEventId = null;
        if (eventId is { } eid)
        {
            var belongs = await db.Events.AnyAsync(e => e.Id == eid && e.CircleId == circleId);
            if (belongs) validEventId = eid;
        }

        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            CircleId = circleId,
            CreatedByPersonId = personId,
            Title = title.Trim(),
            Body = body.Trim(),
            EventId = validEventId,
            CreatedAt = DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        var dto = await db.Announcements
            .Where(a => a.Id == announcement.Id)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.CircleId,
                a.Circle!.Name,
                a.CreatedByPersonId,
                a.CreatedByPerson!.FirstName + " " + a.CreatedByPerson.LastName,
                a.Title,
                a.Body,
                a.CreatedAt,
                a.UpdatedAt,
                a.EventId,
                a.Event != null ? a.Event.Title : null
            ))
            .FirstAsync();

        // Fire-and-forget e-mail notification to circle members.
        DispatchNotification(circleId, dto.Title, dto.CreatedByName, personId);

        return dto;
    }

    /// <summary>
    /// Sends the "new announcement" notification on a background task in its own
    /// DI scope (so it does not use the request-scoped DbContext, which may be
    /// disposed once the request completes). Any failure is logged, never thrown.
    /// </summary>
    private void DispatchNotification(Guid circleId, string title, string authorName, Guid authorPersonId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
                await notifications.NotifyNewAnnouncementAsync(circleId, title, authorName, authorPersonId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kunde inte skicka notis om nytt meddelande i cirkel {CircleId}.", circleId);
            }
        });
    }
}
