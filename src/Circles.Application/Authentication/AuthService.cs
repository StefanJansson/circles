using System.Security.Cryptography;
using Circles.Domain.Entities;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Authentication;

/// <summary>
/// Encapsulates all credential and onboarding logic: registering an account for
/// an existing person, validating a password login, and minting/consuming
/// passwordless "magic link" tokens.
///
/// It deliberately does NOT know about JWTs or HTTP — token issuance lives in
/// the API layer. This service only resolves and verifies identities.
/// </summary>
public class AuthService
{
    private readonly CirclesDbContext _db;
    private readonly IPasswordHasher _hasher;

    // How long a passwordless magic link stays valid after being requested.
    private static readonly TimeSpan MagicLinkLifetime = TimeSpan.FromMinutes(15);

    public AuthService(CirclesDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ---- Registration / onboarding ---------------------------------------

    /// <summary>
    /// Creates a new <see cref="UserAccount"/> and, when a personId is supplied,
    /// links it to that existing <see cref="Person"/>. This is the onboarding
    /// path: people (including children) exist first; an account is claimed for
    /// one of them later.
    /// </summary>
    public async Task<RegisterResult> RegisterAsync(
        string email, string password, Guid? personId, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        if (await _db.UserAccounts.AnyAsync(u => u.Email == email, ct))
            return RegisterResult.Fail("Ett konto med den här e-postadressen finns redan.");

        Person? person = null;
        if (personId is { } pid)
        {
            person = await _db.Persons
                .Include(p => p.UserAccount)
                .FirstOrDefaultAsync(p => p.Id == pid, ct);

            if (person is null)
                return RegisterResult.Fail("Personen kunde inte hittas.");
            if (person.UserAccount is not null)
                return RegisterResult.Fail("Den här personen har redan ett konto.");
        }

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(password),
            PersonId = personId,
            CreatedAt = DateTime.UtcNow
        };
        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync(ct);

        return RegisterResult.Ok(account);
    }

    // ---- Password login ---------------------------------------------------

    /// <summary>Verifies email + password and returns the account on success.</summary>
    public async Task<UserAccount?> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var account = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account is null) return null;
        return _hasher.Verify(password, account.PasswordHash) ? account : null;
    }

    // ---- Passwordless magic link -----------------------------------------

    /// <summary>
    /// Mints a single-use magic link token for the account with the given email.
    /// Returns the raw token when an account exists, otherwise null. Callers
    /// should NOT reveal to the client whether an account was found (to avoid
    /// account enumeration) — the token is delivered out of band (email/SMS).
    /// </summary>
    public async Task<string?> CreateMagicLinkAsync(string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var account = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account is null) return null;

        var token = GenerateToken();
        _db.MagicLinkTokens.Add(new MagicLinkToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserAccountId = account.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(MagicLinkLifetime)
        });
        await _db.SaveChangesAsync(ct);
        return token;
    }

    /// <summary>
    /// Redeems a magic link token. Marks it consumed (single use) and returns the
    /// linked account when the token is valid, unexpired and unconsumed.
    /// </summary>
    public async Task<UserAccount?> ConsumeMagicLinkAsync(string token, CancellationToken ct = default)
    {
        var entry = await _db.MagicLinkTokens
            .Include(t => t.UserAccount)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (entry is null || !entry.IsRedeemableAt(DateTime.UtcNow))
            return null;

        entry.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return entry.UserAccount;
    }

    // ---- Identity resolution ---------------------------------------------

    /// <summary>Loads the account plus its linked person for building "me" responses.</summary>
    public async Task<UserAccount?> GetAccountAsync(Guid userAccountId, CancellationToken ct = default) =>
        await _db.UserAccounts
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == userAccountId, ct);

    /// <summary>
    /// Looks up an account by email (case-insensitive), including the linked
    /// person. Used by external OAuth sign-in to match an existing account —
    /// we never auto-create accounts for a controlled club membership.
    /// </summary>
    public async Task<UserAccount?> GetAccountByEmailAsync(string email, CancellationToken ct = default)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return null;
        return await _db.UserAccounts
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}

/// <summary>Outcome of a registration attempt.</summary>
public record RegisterResult(bool Succeeded, UserAccount? Account, string? Error)
{
    public static RegisterResult Ok(UserAccount account) => new(true, account, null);
    public static RegisterResult Fail(string error) => new(false, null, error);
}
