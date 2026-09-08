using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Events;

public class GetEventsRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/events — list a circle's calendar events.
/// </summary>
public class GetEventsEndpoint(EventService eventService)
    : Endpoint<GetEventsRequest, List<EventDto>>
{
    public override void Configure()
    {
        Get("/api/circles/{circleId}/events");
        Description(b => b.WithTags("Events"));
    }

    public override async Task HandleAsync(GetEventsRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var events = await eventService.GetEventsAsync(personId, req.CircleId);
            await Send.OkAsync(events, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
