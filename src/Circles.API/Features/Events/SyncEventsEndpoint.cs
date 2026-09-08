using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Events;

public class SyncEventsRequest
{
    public Guid CircleId { get; set; }
}

public record SyncEventsResponse(int Imported);

/// <summary>
/// POST /api/circles/{circleId}/events/sync — re-import the circle's events from
/// its laget.se subscription. Requires a managing permission
/// (PublishAnnouncements or AdministerMembers).
/// </summary>
public class SyncEventsEndpoint(EventService eventService)
    : Endpoint<SyncEventsRequest, SyncEventsResponse>
{
    public override void Configure()
    {
        Post("/api/circles/{circleId}/events/sync");
        Description(b => b.WithTags("Events"));
    }

    public override async Task HandleAsync(SyncEventsRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var imported = await eventService.SyncEventsAsync(personId, req.CircleId);
            await Send.OkAsync(new SyncEventsResponse(imported), ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
