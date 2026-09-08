using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Circles.API.Features.Admin;

// ─── POST /api/circles/{circleId}/members ─────────────────────────────────────

public class InviteMemberRequest
{
    public Guid CircleId { get; set; }
    public string Email { get; set; } = string.Empty;
    public MembershipRole Role { get; set; } = MembershipRole.Member;
}

/// <summary>
/// POST /api/circles/{circleId}/members — invite a person to the circle by their
/// account e-mail. Returns 200 with the new member on success, or 400 with a
/// Swedish message when no matching account/person exists or they already belong.
/// </summary>
public class InviteMemberEndpoint(AdminService admin)
    : Endpoint<InviteMemberRequest, InviteResultDto>
{
    public override void Configure()
    {
        Post("/api/circles/{circleId}/members");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(InviteMemberRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, req.CircleId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await admin.InviteMemberAsync(req.CircleId, req.Email, req.Role);
        if (!result.Success)
        {
            AddError(result.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}

// ─── PATCH /api/memberships/{membershipId}/role ───────────────────────────────

public class ChangeRoleRequest
{
    public Guid MembershipId { get; set; }
    public MembershipRole Role { get; set; }
}

/// <summary>
/// PATCH /api/memberships/{membershipId}/role — change a member's role.
/// </summary>
public class ChangeRoleEndpoint(AdminService admin, CirclesDbContext db)
    : Endpoint<ChangeRoleRequest>
{
    public override void Configure()
    {
        Patch("/api/memberships/{membershipId}/role");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(ChangeRoleRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        var circleId = await db.Memberships
            .Where(m => m.Id == req.MembershipId)
            .Select(m => (Guid?)m.CircleId)
            .FirstOrDefaultAsync(ct);

        if (circleId is null) { await Send.NotFoundAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, circleId.Value))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            await admin.ChangeRoleAsync(req.MembershipId, req.Role);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}

// ─── DELETE /api/memberships/{membershipId} ───────────────────────────────────

public class EndMembershipRequest
{
    public Guid MembershipId { get; set; }
}

/// <summary>
/// DELETE /api/memberships/{membershipId} — end a membership by setting
/// ValidUntil = now. The row is kept so historical membership is preserved.
/// </summary>
public class EndMembershipEndpoint(AdminService admin, CirclesDbContext db)
    : Endpoint<EndMembershipRequest>
{
    public override void Configure()
    {
        Delete("/api/memberships/{membershipId}");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(EndMembershipRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        var circleId = await db.Memberships
            .Where(m => m.Id == req.MembershipId)
            .Select(m => (Guid?)m.CircleId)
            .FirstOrDefaultAsync(ct);

        if (circleId is null) { await Send.NotFoundAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, circleId.Value))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            await admin.EndMembershipAsync(req.MembershipId);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
