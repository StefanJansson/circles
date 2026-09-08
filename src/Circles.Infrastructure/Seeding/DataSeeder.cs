using Circles.Domain.Entities;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Infrastructure.Seeding;

/// <summary>
/// Seeds only the data the platform genuinely needs to bootstrap:
///
///   1. The canonical role → permission mapping (system data, not demo data).
///   2. A single site administrator account — the operator of the whole Circles
///      site, who can see and manage every organization.
///
/// There is deliberately NO demo club data. Real organizations, teams, people
/// and memberships are created through the registration wizard and the admin
/// panel. The seed is idempotent: each part is skipped if it already exists.
/// </summary>
public static class DataSeeder
{
    private static Guid Id(string key) =>
        new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key)));

    // ---- Site administrator bootstrap credentials -------------------------
    // The very first account so someone can always log in to a fresh database.
    // The password is hashed with BCrypt at seed time (never stored in plain
    // text). The site admin can then onboard organizations via the wizard.
    private const string SiteAdminEmail = "stefan@veum.se";
    private const string SiteAdminPassword = "Leksand!";

    public static async Task SeedAsync(CirclesDbContext db)
    {
        await SeedRolePermissionsAsync(db);
        await SeedSiteAdminAsync(db);
    }

    /// <summary>
    /// Seeds the canonical role → permission mapping from
    /// <see cref="RolePermissionMap"/>. This is system data required for
    /// authorization to work at all. Idempotent.
    /// </summary>
    private static async Task SeedRolePermissionsAsync(CirclesDbContext db)
    {
        if (await db.RolePermissions.AnyAsync()) return;

        foreach (var (role, permissions) in RolePermissionMap.Map)
        {
            foreach (var permission in permissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Id($"rp:{role}:{permission}"),
                    Role = role,
                    Permission = permission
                });
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the single site administrator account. Idempotent — skipped if an
    /// account with the site admin e-mail already exists.
    /// </summary>
    private static async Task SeedSiteAdminAsync(CirclesDbContext db)
    {
        var email = SiteAdminEmail.Trim().ToLowerInvariant();
        if (await db.UserAccounts.AnyAsync(u => u.Email == email))
            return;

        var account = new UserAccount
        {
            Id = Id("account:site-admin"),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SiteAdminPassword),
            IsSiteAdmin = true,
            PersonId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.UserAccounts.Add(account);

        await db.SaveChangesAsync();
    }
}
