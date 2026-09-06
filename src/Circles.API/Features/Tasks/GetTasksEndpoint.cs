using Circles.Application.DTOs;
using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Tasks;

public class GetTasksRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/tasks — list all tasks for a circle.
/// </summary>
public class GetTasksEndpoint : Endpoint<GetTasksRequest, List<CirclesTaskDto>>
{
    private readonly ContentService _svc;

    public GetTasksEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/circles/{circleId}/tasks");
        Description(b => b.WithTags("Tasks"));
    }

    public override async Task HandleAsync(GetTasksRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var result = await _svc.GetTasksAsync(req.CircleId, personId);
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
