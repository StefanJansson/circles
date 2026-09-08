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
    /// Ensures the single site administrator account exists AND is usable with
    /// the documented bootstrap credentials.
    ///
    /// This is deliberately self-healing rather than a plain "skip if exists":
    /// on a database that already contains an account with this e-mail (e.g. one
    /// created during earlier testing, a magic-link sign-in, or an older seed),
    /// a plain skip would leave that account with the wrong password or without
    /// the site-admin flag — and the documented login would fail. So if the
    /// account already exists we promote it to site admin and reset its password
    /// to the known bootstrap value whenever that value doesn't already verify.
    /// </summary>
    private static async Task SeedSiteAdminAsync(CirclesDbContext db)
    {
        var email = SiteAdminEmail.Trim().ToLowerInvariant();
        var account = await db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

        if (account is null)
        {
            db.UserAccounts.Add(new UserAccount
            {
                Id = Id("account:site-admin"),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SiteAdminPassword),
                IsSiteAdmin = true,
                PersonId = null,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return;
        }

        // Account already exists — make sure it is a working site admin.
        var changed = false;

        if (!account.IsSiteAdmin)
        {
            account.IsSiteAdmin = true;
            changed = true;
        }

        // Reset the password only when the documented one doesn't already work,
        // so we never clobber a hash that is already correct.
        var passwordWorks =
            !string.IsNullOrEmpty(account.PasswordHash) &&
            SafeVerify(SiteAdminPassword, account.PasswordHash);

        if (!passwordWorks)
        {
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(SiteAdminPassword);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync();
    }

    /// <summary>BCrypt verify that never throws on a malformed stored hash.</summary>
    private static bool SafeVerify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}
