using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using FastEndpoints;

namespace Circles.API.Features.Admin;

// ─── GET /api/circles/{circleId}/admin/modules ────────────────────────────────

public class ListModuleStatusRequest
{
    public Guid CircleId { get; set; }
}

/// <summary>
/// GET /api/circles/{circleId}/admin/modules — the enabled/disabled state of every
/// feature module for the organization that owns the circle.
/// </summary>
public class ListModuleStatusEndpoint(AdminService admin)
    : Endpoint<ListModuleStatusRequest, List<ModuleStatusDto>>
{
    public override void Configure()
    {
        Get("/api/circles/{circleId}/admin/modules");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(ListModuleStatusRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, req.CircleId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        await Send.OkAsync(await admin.GetModuleStatusesForCircleAsync(req.CircleId), ct);
    }
}

// ─── PATCH /api/circles/{circleId}/modules/{moduleType}/toggle ─────────────────

public class ToggleModuleRequest
{
    public Guid CircleId { get; set; }
    public string ModuleType { get; set; } = string.Empty;
}

public class ToggleModuleResponse
{
    public ModuleType Module { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// PATCH /api/circles/{circleId}/modules/{moduleType}/toggle — toggle a feature
/// module on/off for the organization that owns the circle. Modules are an
/// organization-level concept, so this affects the whole organization.
/// </summary>
public class ToggleModuleEndpoint(AdminService admin)
    : Endpoint<ToggleModuleRequest, ToggleModuleResponse>
{
    public override void Configure()
    {
        Patch("/api/circles/{circleId}/modules/{moduleType}/toggle");
        Description(b => b.WithTags("Admin"));
    }

    public override async Task HandleAsync(ToggleModuleRequest req, CancellationToken ct)
    {
        var personId = User.PersonId();
        if (personId is null) { await Send.UnauthorizedAsync(ct); return; }

        if (!await admin.CanAdministerCircleAsync(personId.Value, req.CircleId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!Enum.TryParse<ModuleType>(req.ModuleType, ignoreCase: true, out var module))
        {
            AddError("Okänd modul.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        try
        {
            var isEnabled = await admin.ToggleModuleForCircleAsync(req.CircleId, module);
            await Send.OkAsync(new ToggleModuleResponse { Module = module, IsEnabled = isEnabled }, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
