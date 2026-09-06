using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Polls;

public class CreatePollRequest
{
    public Guid CircleId { get; set; }
    public string Question { get; set; } = "";
    public List<string> Options { get; set; } = new();
}

public record CreatePollResponse(Guid Id);

/// <summary>
/// POST /api/circles/{circleId}/polls — create a new poll.
/// Returns the new poll ID.
/// </summary>
public class CreatePollEndpoint : Endpoint<CreatePollRequest, CreatePollResponse>
{
    private readonly ContentService _svc;

    public CreatePollEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Post("/api/circles/{circleId}/polls");
        Description(b => b.WithTags("Polls"));
    }

    public override async Task HandleAsync(CreatePollRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Question))
        {
            AddError("Frågan får inte vara tom.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        if (req.Options == null || req.Options.Count < 2)
        {
            AddError("Minst två svarsalternativ krävs.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        try
        {
            var id = await _svc.CreatePollAsync(req.CircleId, personId, req.Question, req.Options);
            await Send.CreatedAtAsync<GetPollEndpoint>(
                new { id }, new CreatePollResponse(id), cancellation: ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
