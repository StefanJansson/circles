using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using FastEndpoints;
using FluentValidation;

namespace Circles.API.Features.Admin;

// ─── GET /api/organizations/{orgId}/admin/circles ─────────────────────────────

public class ListAdminCirclesRequest
{
    public Guid OrgId { get; set; }
}

/// <summary>
/// GET /api/organizations/{orgId}/admin/circles — all circles in the organization
/// with active-member counts and archive state (for the admin overview).
/// </summary>
public class ListAdminCirclesEndpoint(AdminService admin)
    : Endpoint<ListAdminCirclesRequest, List<AdminCircleDto>>
{
    public override void Configure()
    {
        Get("/api/organizations/{orgId}/admin/circles");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(ListAdminCirclesRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerOrganizationAsync(personId.Value, req.OrgId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        await Send.OkAsync(await admin.GetOrganizationCirclesAsync(req.OrgId), ct);
    }
}

// ─── POST /api/organizations/{orgId}/circles ──────────────────────────────────

public class CreateCircleRequest
{
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CircleType Type { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCircleId { get; set; }
}

public class CreateCircleValidator : Validator<CreateCircleRequest>
{
    public CreateCircleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Namn får inte vara tomt.")
            .MaximumLength(300).WithMessage("Namn får vara max 300 tecken.");
    }
}

/// <summary>
/// POST /api/organizations/{orgId}/circles — create a new circle in an organization.
/// </summary>
public class CreateCircleEndpoint(AdminService admin)
    : Endpoint<CreateCircleRequest, AdminCircleDto>
{
    public override void Configure()
    {
        Post("/api/organizations/{orgId}/circles");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(CreateCircleRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerOrganizationAsync(personId.Value, req.OrgId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var dto = await admin.CreateCircleAsync(
                req.OrgId, req.Name, req.Type, req.Description, req.ParentCircleId);
            await Send.OkAsync(dto, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}

// ─── PATCH /api/circles/{circleId}/archive ────────────────────────────────────

public class ArchiveCircleRequest
{
    public Guid CircleId { get; set; }
    /// <summary>True to archive, false to restore. Defaults to archiving.</summary>
    public bool Archived { get; set; } = true;
}

/// <summary>
/// PATCH /api/circles/{circleId}/archive — archive or restore a circle. Archiving
/// never deletes; the circle and its history are preserved.
/// </summary>
public class ArchiveCircleEndpoint(AdminService admin)
    : Endpoint<ArchiveCircleRequest>
{
    public override void Configure()
    {
        Patch("/api/circles/{circleId}/archive");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(ArchiveCircleRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, req.CircleId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            await admin.SetCircleArchivedAsync(req.CircleId, req.Archived);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
