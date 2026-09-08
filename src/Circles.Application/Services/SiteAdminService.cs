using Circles.Application.DTOs;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

/// <summary>
/// Read/administration operations reserved for the platform site administrator
/// — the operator who sits ABOVE all organizations. Unlike <see cref="AdminService"/>
/// (which is scoped to a person's memberships), these operations span every
/// organization on the site.
/// </summary>
public class SiteAdminService
{
    private readonly CirclesDbContext _db;

    public SiteAdminService(CirclesDbContext db)
    {
        _db = db;
    }

    /// <summary>True when the given account is flagged as a site administrator.</summary>
    public async Task<bool> IsSiteAdminAsync(Guid userAccountId)
    {
        return await _db.UserAccounts
            .AnyAsync(u => u.Id == userAccountId && u.IsSiteAdmin);
    }

    /// <summary>
    /// Lists every organization on the platform with a count of its circles and
    /// currently-active members, most recently created first.
    /// </summary>
    public async Task<List<SiteOrganizationDto>> GetAllOrganizationsAsync()
    {
        var now = DateTime.UtcNow;

        return await _db.Organizations
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new SiteOrganizationDto(
                o.Id,
                o.Name,
                o.Slug,
                o.Circles.Count(),
                o.Circles
                    .SelectMany(c => c.Memberships)
                    .Count(m => m.ValidFrom <= now
                                && (m.ValidUntil == null || m.ValidUntil > now)),
                o.CreatedAt))
            .ToListAsync();
    }
}
