using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Discussions;

public class CreateDiscussionRequest
{
    public Guid CircleId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    // Optional link to a calendar event this discussion concerns (Task 7b).
    public Guid? EventId { get; set; }
}

public record CreateDiscussionResponse(Guid Id);

/// <summary>
/// POST /api/circles/{circleId}/discussions — start a new discussion thread.
/// Returns the new discussion ID.
/// </summary>
public class CreateDiscussionEndpoint : Endpoint<CreateDiscussionRequest, CreateDiscussionResponse>
{
    private readonly ContentService _svc;

    public CreateDiscussionEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Post("/api/circles/{circleId}/discussions");
        Description(b => b.WithTags("Discussions"));
    }

    public override async Task HandleAsync(CreateDiscussionRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Body))
        {
            AddError("Titel och innehåll krävs.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        try
        {
            var id = await _svc.CreateDiscussionAsync(
                req.CircleId, personId, req.Title, req.Body, req.EventId);
            await Send.CreatedAtAsync<GetDiscussionEndpoint>(
                new { id }, new CreateDiscussionResponse(id), cancellation: ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
