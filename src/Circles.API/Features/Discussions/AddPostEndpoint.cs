using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Discussions;

public class AddPostRequest
{
    public Guid Id { get; set; }
    public string Body { get; set; } = "";
}

public record AddPostResponse(Guid PostId);

/// <summary>
/// POST /api/discussions/{id}/posts — add a reply post to a discussion thread.
/// Returns the new post ID.
/// </summary>
public class AddPostEndpoint : Endpoint<AddPostRequest, AddPostResponse>
{
    private readonly ContentService _svc;

    public AddPostEndpoint(ContentService svc) => _svc = svc;

    public override void Configure()
    {
        Post("/api/discussions/{id}/posts");
        Description(b => b.WithTags("Discussions"));
    }

    public override async Task HandleAsync(AddPostRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Body))
        {
            AddError("Svaret får inte vara tomt.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        try
        {
            var postId = await _svc.AddPostAsync(req.Id, personId, req.Body);
            await Send.CreatedAtAsync<GetDiscussionEndpoint>(
                new { id = req.Id }, new AddPostResponse(postId), cancellation: ct);
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
