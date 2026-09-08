using System.Text;
using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

/// <summary>
/// Handles first-time onboarding of a brand-new organization through the
/// registration wizard. In a single unit of work it creates:
///
///   • the <see cref="Organization"/> (with a unique slug),
///   • its root <see cref="Circle"/>,
///   • the administrator's <see cref="Person"/> and <see cref="UserAccount"/>,
///   • an Administrator <see cref="Membership"/> on the root circle,
///   • one team <see cref="Circle"/> per supplied team name,
///   • all feature modules enabled for the organization.
///
/// This is the only self-service entry point that creates an account without a
/// pre-existing person — every other membership flows through the admin panel.
/// </summary>
public class OnboardingService
{
    private readonly CirclesDbContext _db;
    private readonly IPasswordHasher _hasher;

    public OnboardingService(CirclesDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<OnboardingResult> RegisterOrganizationAsync(
        OnboardingRequest request, CancellationToken ct = default)
    {
        // ---- Validate input ------------------------------------------------
        var orgName = (request.OrganizationName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(orgName))
            return OnboardingResult.Fail("Organisationens namn får inte vara tomt.");

        var firstName = (request.AdminFirstName ?? "").Trim();
        var lastName = (request.AdminLastName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return OnboardingResult.Fail("Administratörens för- och efternamn får inte vara tomt.");

        var email = (request.AdminEmail ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return OnboardingResult.Fail("Ange en giltig e-postadress.");

        var password = request.AdminPassword ?? "";
        if (password.Length < 8)
            return OnboardingResult.Fail("Lösenordet måste vara minst 8 tecken.");

        if (await _db.UserAccounts.AnyAsync(u => u.Email == email, ct))
            return OnboardingResult.Fail("Ett konto med den här e-postadressen finns redan.");

        var teamNames = (request.TeamNames ?? new List<string>())
            .Select(t => (t ?? "").Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // ---- Organization --------------------------------------------------
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = orgName,
            Slug = await GenerateUniqueOrgSlugAsync(orgName, ct),
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);

        // ---- Root circle (the whole club) ----------------------------------
        var root = new Circle
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            ParentCircleId = null,
            Name = orgName,
            Slug = Slugify(orgName) is { Length: > 0 } s ? s : "klubb",
            Type = CircleType.General,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Circles.Add(root);

        // ---- Admin person + account ----------------------------------------
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };
        _db.Persons.Add(person);

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(password),
            IsSiteAdmin = false,
            PersonId = person.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.UserAccounts.Add(account);

        // ---- Administrator membership on the root circle -------------------
        _db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            CircleId = root.Id,
            Role = MembershipRole.Administrator,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = null
        });

        // ---- Team circles --------------------------------------------------
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.Slug };
        foreach (var teamName in teamNames)
        {
            var slug = UniqueSlugAmong(teamName, usedSlugs);
            usedSlugs.Add(slug);

            _db.Circles.Add(new Circle
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                ParentCircleId = root.Id,
                Name = teamName,
                Slug = slug,
                Type = CircleType.Team,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // ---- Enable all feature modules for the new organization -----------
        foreach (ModuleType module in Enum.GetValues<ModuleType>())
        {
            _db.OrganizationModules.Add(new OrganizationModule
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Module = module,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        return OnboardingResult.Ok(account.Id, org.Id);
    }

    // ---- Slug helpers -----------------------------------------------------

    private async Task<string> GenerateUniqueOrgSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "organisation";

        var slug = baseSlug;
        var suffix = 2;
        while (await _db.Organizations.AnyAsync(o => o.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
    }

    private static string UniqueSlugAmong(string name, HashSet<string> used)
    {
        var baseSlug = Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "cirkel";

        var slug = baseSlug;
        var suffix = 2;
        while (used.Contains(slug))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
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

/// <summary>Input for onboarding a new organization via the wizard.</summary>
public record OnboardingRequest(
    string? OrganizationName,
    string? AdminFirstName,
    string? AdminLastName,
    string? AdminEmail,
    string? AdminPassword,
    List<string>? TeamNames);

/// <summary>Outcome of an onboarding attempt.</summary>
public record OnboardingResult(bool Succeeded, Guid? AccountId, Guid? OrganizationId, string? Error)
{
    public static OnboardingResult Ok(Guid accountId, Guid orgId) =>
        new(true, accountId, orgId, null);
    public static OnboardingResult Fail(string error) =>
        new(false, null, null, error);
}
