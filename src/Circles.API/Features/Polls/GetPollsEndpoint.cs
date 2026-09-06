using Circles.Application.DTOs;
using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Polls;

public class GetPollsRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/polls — list all polls in a circle.
/// </summary>
public class GetPollsEndpoint : Endpoint<GetPollsRequest, List<PollSummaryDto>>
{
    private readonly ContentService _svc;

    public GetPollsEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/circles/{circleId}/polls");
        Description(b => b.WithTags("Polls"));
    }

    public override async Task HandleAsync(GetPollsRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var result = await _svc.GetPollsAsync(req.CircleId, personId);
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
