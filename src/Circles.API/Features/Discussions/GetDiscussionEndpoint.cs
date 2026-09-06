using Circles.Application.DTOs;
using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Discussions;

public class GetDiscussionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// GET /api/discussions/{id} — full discussion thread with all posts.
/// </summary>
public class GetDiscussionEndpoint : Endpoint<GetDiscussionRequest, DiscussionDetailDto>
{
    private readonly ContentService _svc;

    public GetDiscussionEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/discussions/{id}");
        Description(b => b.WithTags("Discussions"));
    }

    public override async Task HandleAsync(GetDiscussionRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var result = await _svc.GetDiscussionAsync(req.Id, personId);
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
