using System.Text;
using Circles.Application.DTOs;
using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

/// <summary>
/// Administrative operations for circles, memberships and feature modules.
/// Backs both the admin API endpoints and the Blazor admin panel.
///
/// Business rules that would corrupt the domain (e.g. inviting an unknown
/// e-mail, ending a non-existent membership) are signalled with an
/// <see cref="InvalidOperationException"/> carrying a Swedish, user-facing
/// message. Callers are responsible for authorization.
/// </summary>
public class AdminService(CirclesDbContext db, IAuthorizationService authz)
{
    // ─── Authorization helpers ────────────────────────────────────────────────

    /// <summary>
    /// The circles a person is allowed to administer, as admin DTOs. A person can
    /// administer a circle when they hold the <see cref="PermissionType.AdministerMembers"/>
    /// permission on it. Additionally, holding that permission on an organization's
    /// ROOT circle grants administration of every circle in that organization.
    /// Archived circles are included so they can be restored.
    /// </summary>
    public async Task<List<AdminCircleDto>> GetAdministrableCirclesAsync(Guid personId)
    {
        var now = DateTime.UtcNow;

        // Circles the person can reach at all (direct or derived).
        var accessibleIds = await authz.GetAccessibleCircleIdsAsync(personId, now);
        if (accessibleIds.Count == 0)
            return new List<AdminCircleDto>();

        var accessibleCircles = await db.Circles
            .Where(c => accessibleIds.Contains(c.Id))
            .Select(c => new { c.Id, c.OrganizationId, c.ParentCircleId })
            .ToListAsync();

        var adminCircleIds = new HashSet<Guid>();
        var adminOrgIds = new HashSet<Guid>();

        foreach (var c in accessibleCircles)
        {
            var perms = await authz.GetPersonPermissionsInCircleAsync(personId, c.Id, now);
            if (perms.Contains(PermissionType.AdministerMembers))
            {
                adminCircleIds.Add(c.Id);
                // AdministerMembers on a root circle => administer the whole org.
                if (c.ParentCircleId is null)
                    adminOrgIds.Add(c.OrganizationId);
            }
        }

        if (adminCircleIds.Count == 0)
            return new List<AdminCircleDto>();

        return await db.Circles
            .Where(c => adminOrgIds.Contains(c.OrganizationId) || adminCircleIds.Contains(c.Id))
            .OrderBy(c => c.IsArchived)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCircleDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.Slug,
                c.Type,
                c.Description,
                c.ParentCircleId,
                c.Memberships.Count(m => m.ValidFrom <= now
                                         && (m.ValidUntil == null || m.ValidUntil > now)),
                c.IsArchived))
            .ToListAsync();
    }

    /// <summary>True if the person may administer the given circle.</summary>
    public async Task<bool> CanAdministerCircleAsync(Guid personId, Guid circleId)
    {
        var now = DateTime.UtcNow;

        var perms = await authz.GetPersonPermissionsInCircleAsync(personId, circleId, now);
        if (perms.Contains(PermissionType.AdministerMembers))
            return true;

        // Root-circle administrators can administer every circle in their org.
        var orgId = await db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => (Guid?)c.OrganizationId)
            .FirstOrDefaultAsync();
        if (orgId is null)
            return false;

        var rootCircleIds = await db.Circles
            .Where(c => c.OrganizationId == orgId && c.ParentCircleId == null)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var rootId in rootCircleIds)
        {
            var rootPerms = await authz.GetPersonPermissionsInCircleAsync(personId, rootId, now);
            if (rootPerms.Contains(PermissionType.AdministerMembers))
                return true;
        }
        return false;
    }

    /// <summary>True if the person may administer at least one circle (for nav).</summary>
    public async Task<bool> CanAccessAdminAsync(Guid personId) =>
        (await GetAdministrableCirclesAsync(personId)).Count > 0;

    /// <summary>The organizations the person may administer (for the create-circle form).</summary>
    public async Task<List<OrganizationDto>> GetAdministrableOrganizationsAsync(Guid personId)
    {
        var administrable = await GetAdministrableCirclesAsync(personId);
        var orgIds = administrable.Select(c => c.OrganizationId).Distinct().ToList();
        if (orgIds.Count == 0)
            return new List<OrganizationDto>();

        return await db.Organizations
            .Where(o => orgIds.Contains(o.Id))
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationDto(o.Id, o.Name, o.Slug))
            .ToListAsync();
    }

    /// <summary>True if the person may administer any circle in the organization.</summary>
    public async Task<bool> CanAdministerOrganizationAsync(Guid personId, Guid organizationId)
    {
        var administrable = await GetAdministrableCirclesAsync(personId);
        if (administrable.Count == 0)
            return false;

        var administrableIds = administrable.Select(c => c.Id).ToHashSet();
        var orgCircleIds = await db.Circles
            .Where(c => c.OrganizationId == organizationId)
            .Select(c => c.Id)
            .ToListAsync();

        return orgCircleIds.Any(administrableIds.Contains);
    }

    // ─── Circles ────────────────────────────────────────────────────────────

    /// <summary>All circles in an organization, with active-member counts.</summary>
    public async Task<List<AdminCircleDto>> GetOrganizationCirclesAsync(
        Guid organizationId, bool includeArchived = true)
    {
        var now = DateTime.UtcNow;

        var query = db.Circles.Where(c => c.OrganizationId == organizationId);
        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        return await query
            .OrderBy(c => c.IsArchived)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCircleDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.Slug,
                c.Type,
                c.Description,
                c.ParentCircleId,
                c.Memberships.Count(m => m.ValidFrom <= now
                                         && (m.ValidUntil == null || m.ValidUntil > now)),
                c.IsArchived))
            .ToListAsync();
    }

    /// <summary>A single circle for the admin detail page, or null if missing.</summary>
    public async Task<AdminCircleDto?> GetCircleAsync(Guid circleId)
    {
        var now = DateTime.UtcNow;
        return await db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => new AdminCircleDto(
                c.Id,
                c.OrganizationId,
                c.Name,
                c.Slug,
                c.Type,
                c.Description,
                c.ParentCircleId,
                c.Memberships.Count(m => m.ValidFrom <= now
                                         && (m.ValidUntil == null || m.ValidUntil > now)),
                c.IsArchived))
            .FirstOrDefaultAsync();
    }

    /// <summary>Active members of a circle, ordered by role then name.</summary>
    public async Task<List<MemberDto>> GetCircleMembersAsync(Guid circleId, bool includeEnded = false)
    {
        var now = DateTime.UtcNow;
        var query = db.Memberships
            .Where(m => m.CircleId == circleId);

        if (!includeEnded)
            query = query.Where(m => m.ValidFrom <= now
                                     && (m.ValidUntil == null || m.ValidUntil > now));

        return await query
            .OrderBy(m => m.Role)
            .ThenBy(m => m.Person!.FullName)
            .Select(m => new MemberDto(
                m.Id,
                m.PersonId,
                m.Person!.FullName,
                m.Role,
                m.ValidFrom,
                m.ValidUntil))
            .ToListAsync();
    }

    /// <summary>Create a new circle in the organization.</summary>
    public async Task<AdminCircleDto> CreateCircleAsync(
        Guid organizationId,
        string name,
        CircleType type,
        string? description = null,
        Guid? parentCircleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Namn får inte vara tomt.");

        var orgExists = await db.Organizations.AnyAsync(o => o.Id == organizationId);
        if (!orgExists)
            throw new InvalidOperationException("Organisationen kunde inte hittas.");

        if (parentCircleId is { } parentId)
        {
            var parentInOrg = await db.Circles
                .AnyAsync(c => c.Id == parentId && c.OrganizationId == organizationId);
            if (!parentInOrg)
                throw new InvalidOperationException("Den valda överordnade cirkeln tillhör inte organisationen.");
        }

        var circle = new Circle
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ParentCircleId = parentCircleId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Type = type,
            Slug = await GenerateUniqueSlugAsync(organizationId, name),
            IsArchived = false,
        };

        db.Circles.Add(circle);
        await db.SaveChangesAsync();

        return new AdminCircleDto(
            circle.Id, circle.OrganizationId, circle.Name, circle.Slug, circle.Type,
            circle.Description, circle.ParentCircleId, 0, circle.IsArchived);
    }

    /// <summary>Archive or restore a circle (reversible, never deletes).</summary>
    public async Task SetCircleArchivedAsync(Guid circleId, bool archived)
    {
        var circle = await db.Circles.FirstOrDefaultAsync(c => c.Id == circleId)
            ?? throw new InvalidOperationException("Cirkeln kunde inte hittas.");

        circle.IsArchived = archived;
        await db.SaveChangesAsync();
    }

    // ─── Memberships ──────────────────────────────────────────────────────────

    /// <summary>
    /// Invite a person to a circle by their account e-mail. Looks up the
    /// <see cref="UserAccount"/>, resolves the linked <see cref="Person"/>, and
    /// creates an active <see cref="Membership"/>. Returns a Swedish message when
    /// no matching account/person exists or the person is already a member.
    /// </summary>
    public async Task<InviteResultDto> InviteMemberAsync(
        Guid circleId, string email, MembershipRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new InviteResultDto(false, "E-postadress får inte vara tom.", null);

        var circle = await db.Circles.FirstOrDefaultAsync(c => c.Id == circleId);
        if (circle is null)
            return new InviteResultDto(false, "Cirkeln kunde inte hittas.", null);

        var normalized = email.Trim().ToLowerInvariant();
        var account = await db.UserAccounts
            .Include(a => a.Person)
            .FirstOrDefaultAsync(a => a.Email.ToLower() == normalized);

        if (account is null)
            return new InviteResultDto(false,
                $"Ingen användare med e-postadressen \"{email.Trim()}\" hittades.", null);

        if (account.PersonId is not { } personId || account.Person is null)
            return new InviteResultDto(false,
                "Kontot är inte kopplat till en person och kan inte bli medlem.", null);

        var now = DateTime.UtcNow;
        var alreadyMember = await db.Memberships.AnyAsync(m =>
            m.CircleId == circleId
            && m.PersonId == personId
            && m.ValidFrom <= now
            && (m.ValidUntil == null || m.ValidUntil > now));

        if (alreadyMember)
            return new InviteResultDto(false,
                $"{account.Person.FullName} är redan medlem i cirkeln.", null);

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CircleId = circleId,
            Role = role,
            ValidFrom = now,
            ValidUntil = null,
        };
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var dto = new MemberDto(
            membership.Id, personId, account.Person.FullName,
            role, membership.ValidFrom, membership.ValidUntil);

        return new InviteResultDto(true,
            $"{account.Person.FullName} lades till som {role}.", dto);
    }

    /// <summary>Change the role of an existing membership.</summary>
    public async Task ChangeRoleAsync(Guid membershipId, MembershipRole role)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId)
            ?? throw new InvalidOperationException("Medlemskapet kunde inte hittas.");

        membership.Role = role;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// End a membership by setting <c>ValidUntil = now</c>. The row is kept so
    /// historical membership is preserved (memberships are never deleted).
    /// </summary>
    public async Task EndMembershipAsync(Guid membershipId)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId)
            ?? throw new InvalidOperationException("Medlemskapet kunde inte hittas.");

        if (membership.ValidUntil is null || membership.ValidUntil > DateTime.UtcNow)
            membership.ValidUntil = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    // ─── Modules (per organization) ───────────────────────────────────────────

    /// <summary>
    /// The status of every feature module for the organization that owns the
    /// given circle. Modules are an organization-level concept; toggling here
    /// affects the whole organization.
    /// </summary>
    public async Task<List<ModuleStatusDto>> GetModuleStatusesForCircleAsync(Guid circleId)
    {
        var organizationId = await db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => (Guid?)c.OrganizationId)
            .FirstOrDefaultAsync();

        if (organizationId is null)
            return new List<ModuleStatusDto>();

        var enabled = await db.OrganizationModules
            .Where(m => m.OrganizationId == organizationId && m.IsEnabled)
            .Select(m => m.Module)
            .ToListAsync();

        var enabledSet = enabled.ToHashSet();

        return Enum.GetValues<ModuleType>()
            .Select(m => new ModuleStatusDto(m, enabledSet.Contains(m)))
            .ToList();
    }

    /// <summary>
    /// Toggle a module on/off for the organization that owns the given circle.
    /// Creates the module row on first toggle. Returns the new enabled state.
    /// </summary>
    public async Task<bool> ToggleModuleForCircleAsync(Guid circleId, ModuleType module)
    {
        var organizationId = await db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => (Guid?)c.OrganizationId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Cirkeln kunde inte hittas.");

        var row = await db.OrganizationModules
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Module == module);

        if (row is null)
        {
            row = new OrganizationModule
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Module = module,
                IsEnabled = true,
            };
            db.OrganizationModules.Add(row);
        }
        else
        {
            row.IsEnabled = !row.IsEnabled;
        }

        await db.SaveChangesAsync();
        return row.IsEnabled;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// True if the person holds a leadership role (Coach, Leader or Administrator)
    /// in any active membership — used to decide whether to show the admin link.
    /// </summary>
    public async Task<bool> IsAdminForAnyCircleAsync(Guid personId)
    {
        var now = DateTime.UtcNow;
        return await db.Memberships.AnyAsync(m =>
            m.PersonId == personId
            && m.ValidFrom <= now
            && (m.ValidUntil == null || m.ValidUntil > now)
            && (m.Role == MembershipRole.Coach
                || m.Role == MembershipRole.Leader
                || m.Role == MembershipRole.Administrator));
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid organizationId, string name)
    {
        var baseSlug = Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "cirkel";

        var slug = baseSlug;
        var suffix = 2;
        while (await db.Circles.AnyAsync(c => c.OrganizationId == organizationId && c.Slug == slug))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        // Map common Swedish characters to ASCII.
        normalized = normalized
            .Replace('å', 'a').Replace('ä', 'a').Replace('ö', 'o')
            .Replace('é', 'e').Replace('ü', 'u');

        var sb = new StringBuilder(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
