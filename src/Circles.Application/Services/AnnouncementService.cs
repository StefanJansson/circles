using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
    DateTime? UpdatedAt
);

public class AnnouncementService(CirclesDbContext db, IAuthorizationService authz)
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
                a.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(
        Guid personId, Guid circleId, string title, string body)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.PublishAnnouncements);

        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            CircleId = circleId,
            CreatedByPersonId = personId,
            Title = title.Trim(),
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        return await db.Announcements
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
                a.UpdatedAt
            ))
            .FirstAsync();
    }
}
