using Circles.Application.Services;
using FastEndpoints;

namespace Circles.API.Features.Modules;

public class GetModulesRequest
{
    public Guid OrgId { get; set; }
}

/// <summary>
/// GET /api/organizations/{orgId}/modules — list the feature modules that the
/// organization has opted in to. Returns the enabled module names.
/// </summary>
public class GetModulesEndpoint(ModuleService moduleService)
    : Endpoint<GetModulesRequest, List<string>>
{
    public override void Configure()
    {
        Get("/api/organizations/{orgId}/modules");
        Description(b => b.WithTags("Modules"));
    }

    public override async Task HandleAsync(GetModulesRequest req, CancellationToken ct)
    {
        var personIdClaim = User.FindFirst("pid")?.Value;
        if (!Guid.TryParse(personIdClaim, out _))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var modules = await moduleService.GetEnabledModulesAsync(req.OrgId);
        await Send.OkAsync(modules.Select(m => m.ToString()).ToList(), ct);
    }
}
