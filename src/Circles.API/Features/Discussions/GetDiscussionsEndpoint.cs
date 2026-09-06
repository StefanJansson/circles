using Circles.Application.DTOs;
using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Discussions;

public class GetDiscussionsRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/discussions — list all discussions in a circle.
/// </summary>
public class GetDiscussionsEndpoint : Endpoint<GetDiscussionsRequest, List<DiscussionSummaryDto>>
{
    private readonly ContentService _svc;

    public GetDiscussionsEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/circles/{circleId}/discussions");
        Description(b => b.WithTags("Discussions"));
    }

    public override async Task HandleAsync(GetDiscussionsRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var result = await _svc.GetDiscussionsAsync(req.CircleId, personId);
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
