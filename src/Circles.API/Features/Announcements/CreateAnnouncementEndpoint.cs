using Circles.Application.Services;
using FastEndpoints;
using FluentValidation;

namespace Circles.API.Features.Announcements;

public class CreateAnnouncementRequest
{
    public Guid CircleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Optional link to a calendar event this announcement concerns (Task 7b).
    public Guid? EventId { get; set; }
}

public class CreateAnnouncementValidator : Validator<CreateAnnouncementRequest>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Rubrik får inte vara tom.")
            .MaximumLength(200).WithMessage("Rubrik får vara max 200 tecken.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Innehåll får inte vara tomt.");
    }
}

/// <summary>
/// POST /api/circles/{circleId}/announcements — create a new announcement (PublishAnnouncements permission required).
/// </summary>
public class CreateAnnouncementEndpoint(AnnouncementService announcementService)
    : Endpoint<CreateAnnouncementRequest, AnnouncementDto>
{
    public override void Configure()
    {
        Post("/api/circles/{circleId}/announcements");
        Description(b => b.WithTags("Announcements"));
    }

    public override async Task HandleAsync(CreateAnnouncementRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out var personId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var announcement = await announcementService.CreateAnnouncementAsync(
                personId, req.CircleId, req.Title, req.Body, req.EventId);
            await Send.CreatedAtAsync<GetAnnouncementsEndpoint>(
                new { circleId = req.CircleId }, announcement, cancellation: ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
