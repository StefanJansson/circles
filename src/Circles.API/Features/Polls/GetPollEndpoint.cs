using Circles.Application.DTOs;
using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Polls;

public class GetPollRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// GET /api/polls/{id} — full poll detail with options and vote counts.
/// </summary>
public class GetPollEndpoint : Endpoint<GetPollRequest, PollDetailDto>
{
    private readonly ContentService _svc;

    public GetPollEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/polls/{id}");
        Description(b => b.WithTags("Polls"));
    }

    public override async Task HandleAsync(GetPollRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var result = await _svc.GetPollAsync(req.Id, personId);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
