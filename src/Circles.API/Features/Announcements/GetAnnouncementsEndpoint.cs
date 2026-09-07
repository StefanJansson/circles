using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Announcements;

public class GetAnnouncementsRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/announcements — list all announcements in a circle.
/// </summary>
public class GetAnnouncementsEndpoint(AnnouncementService announcementService)
    : Endpoint<GetAnnouncementsRequest, List<AnnouncementDto>>
{
    public override void Configure()
    {
        Get("/api/circles/{circleId}/announcements");
        Description(b => b.WithTags("Announcements"));
    }

    public override async Task HandleAsync(GetAnnouncementsRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var announcements = await announcementService.GetAnnouncementsAsync(personId, req.CircleId);
            await Send.OkAsync(announcements, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
