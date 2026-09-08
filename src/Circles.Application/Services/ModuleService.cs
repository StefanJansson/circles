using Circles.Domain.Enums;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

/// <summary>
/// Reads the opt-in feature modules configured for an organization. Modules are
/// enabled per club, so the UI and API can hide features (events, attendance, …)
/// for organizations that have not switched them on.
/// </summary>
public class ModuleService(CirclesDbContext db)
{
    /// <summary>True if the given module is enabled for the organization.</summary>
    public async Task<bool> IsEnabledAsync(Guid organizationId, ModuleType module)
    {
        return await db.OrganizationModules
            .AnyAsync(m => m.OrganizationId == organizationId
                        && m.Module == module
                        && m.IsEnabled);
    }

    /// <summary>All modules enabled for the organization.</summary>
    public async Task<List<ModuleType>> GetEnabledModulesAsync(Guid organizationId)
    {
        return await db.OrganizationModules
            .Where(m => m.OrganizationId == organizationId && m.IsEnabled)
            .Select(m => m.Module)
            .ToListAsync();
    }

    /// <summary>
    /// All modules enabled for the organization that owns the given circle.
    /// Returns an empty list if the circle does not exist.
    /// </summary>
    public async Task<List<ModuleType>> GetEnabledModulesForCircleAsync(Guid circleId)
    {
        var organizationId = await db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => (Guid?)c.OrganizationId)
            .FirstOrDefaultAsync();

        if (organizationId is null)
            return new List<ModuleType>();

        return await GetEnabledModulesAsync(organizationId.Value);
    }
}
