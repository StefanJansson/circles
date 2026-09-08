using System.Security.Claims;

namespace Circles.API.Features.Admin;

/// <summary>
/// Small helpers shared by the admin endpoints for reading the caller's person id
/// off the authenticated principal.
/// </summary>
internal static class AdminEndpointExtensions
{
    public static Guid? PersonId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst("pid")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
