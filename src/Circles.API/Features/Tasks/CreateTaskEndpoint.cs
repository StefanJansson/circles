using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Tasks;

public class CreateTaskRequest
{
    public Guid CircleId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }

    // Task 7c — optional parent task (makes this a sub-task) and assignee.
    public Guid? ParentTaskId { get; set; }
    public Guid? AssignedToPersonId { get; set; }
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
                req.CircleId, personId, req.Title, req.Description ?? "", req.DueDate,
                req.ParentTaskId, req.AssignedToPersonId);
            await Send.CreatedAtAsync<GetTasksEndpoint>(
                new { circleId = req.CircleId }, new CreateTaskResponse(id), cancellation: ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
