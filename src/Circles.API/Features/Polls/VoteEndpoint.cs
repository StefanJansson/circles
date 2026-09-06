using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Polls;

public class VoteRequest
{
    public Guid Id { get; set; }
    public Guid OptionId { get; set; }
}

public class VoteResponse
{
    public bool Success { get; set; }
}

/// <summary>
/// POST /api/polls/{id}/vote — cast or change vote on a poll option.
/// </summary>
public class VoteEndpoint : Endpoint<VoteRequest, VoteResponse>
{
    private readonly ContentService _svc;

    public VoteEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Post("/api/polls/{id}/vote");
        Description(b => b.WithTags("Polls"));
    }

    public override async Task HandleAsync(VoteRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            await _svc.VoteAsync(req.Id, req.OptionId, personId);
            await Send.OkAsync(new VoteResponse { Success = true }, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(422, ct);
        }
    }
}
