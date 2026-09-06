using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Tasks;

public class CreateTaskRequest
{
    public Guid CircleId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
}

public record CreateTaskResponse(Guid Id);

/// <summary>
/// POST /api/circles/{circleId}/tasks — create a new task in a circle.
/// Returns the new task ID.
/// </summary>
public class CreateTaskEndpoint : Endpoint<CreateTaskRequest, CreateTaskResponse>
{
    private readonly ContentService _svc;

    public CreateTaskEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Post("/api/circles/{circleId}/tasks");
        Description(b => b.WithTags("Tasks"));
    }

    public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Title))
        {
            AddError("Uppgiftstitel krävs.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        try
        {
            var id = await _svc.CreateTaskAsync(
                req.CircleId, personId, req.Title, req.Description ?? "", req.DueDate);
            await Send.CreatedAtAsync<GetTasksEndpoint>(
                new { circleId = req.CircleId }, new CreateTaskResponse(id), cancellation: ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
