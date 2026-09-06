using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Tasks;

public class CompleteTaskRequest
{
    public Guid Id { get; set; }
}

public class CompleteTaskResponse
{
    public bool Success { get; set; }
}

/// <summary>
/// PATCH /api/tasks/{id}/complete — mark a task as completed.
/// </summary>
public class CompleteTaskEndpoint : Endpoint<CompleteTaskRequest, CompleteTaskResponse>
{
    private readonly ContentService _svc;

    public CompleteTaskEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Patch("/api/tasks/{id}/complete");
        Description(b => b.WithTags("Tasks"));
    }

    public override async Task HandleAsync(CompleteTaskRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            await _svc.CompleteTaskAsync(req.Id, personId);
            await Send.OkAsync(new CompleteTaskResponse { Success = true }, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
