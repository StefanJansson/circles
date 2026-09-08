using System.Security.Claims;
using Circles.Application.Authentication;
using Circles.Application.Authorization;
using Circles.Application.Services;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Circles.Infrastructure.Security;
using Circles.Infrastructure.Seeding;
using Circles.Infrastructure.Sync;
using Circles.Web.Auth;
using Circles.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Culture ---------------------------------------------------------------
// Render all dates, numbers and month names in Swedish (e.g. "8 september
// 2026", "lör 18 jan") regardless of the host's locale. Set as the process
// default so every request thread and background task formats consistently.
var swedishCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = swedishCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = swedishCulture;

// ---- Blazor (interactive server components) --------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---- Authentication --------------------------------------------------------
// Cookie-based auth handled entirely server-side. Because this is a Blazor
// Server app talking to the Application layer in-process, there is NO JWT and
// NO token stored in the browser — the session lives in an encrypted cookie.
//
// External providers (Google / Facebook) can be added later by chaining
// .AddGoogle(...) / .AddFacebook(...) here; the existing cookie remains the
// primary application session, so the rest of the app is unaffected.
var authBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.Name = "circles.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// ---- External OAuth providers (optional) -----------------------------------
// Google + Microsoft sign-in are enabled ONLY when a ClientId is configured
// (appsettings / environment). Each provider uses a dedicated cookie
// ("circles.ext") purely to carry the external identity through the callback;
// the real application session is always the "circles.auth" cookie built after
// we match the external email to an EXISTING UserAccount. We never auto-create
// accounts (controlled club membership).
const string ExternalScheme = "circles.ext";

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId);

var msClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var msClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
var msEnabled = !string.IsNullOrWhiteSpace(msClientId);

if (googleEnabled || msEnabled)
{
    // Intermediate cookie that temporarily holds the external identity between
    // the provider redirect and our /auth/external-callback handler.
    authBuilder.AddCookie(ExternalScheme, o =>
    {
        o.Cookie.Name = "circles.ext";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });
}

if (googleEnabled)
{
    authBuilder.AddGoogle(o =>
    {
        o.ClientId = googleClientId!;
        o.ClientSecret = googleClientSecret ?? "";
        o.CallbackPath = "/signin-google";
        o.SignInScheme = ExternalScheme;
        o.SaveTokens = false;
    });
}

if (msEnabled)
{
    authBuilder.AddMicrosoftAccount(o =>
    {
        o.ClientId = msClientId!;
        o.ClientSecret = msClientSecret ?? "";
        o.CallbackPath = "/signin-microsoft";
        o.SignInScheme = ExternalScheme;
        o.SaveTokens = false;
    });
}

// Surface which providers are enabled to the UI (Login page) via config lookup.
builder.Services.AddSingleton(new ExternalAuthOptions(googleEnabled, msEnabled));

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ---- Data + application services -------------------------------------------
// EF Core / Azure SQL (SQL Server). Connection string is resolved in priority
// order:
//   1. ConnectionStrings:Circles   (appsettings / ConnectionStrings__Circles env var)
//   2. AZURE_SQL_CONNECTIONSTRING   (the name Azure App Service sets by default)
//   3. a local SQL Server fallback so the prototype runs out of the box.
// Empty / whitespace values are treated as "not set". In Azure, prefer a
// passwordless connection using a managed identity (Authentication=Active Directory Default).
var connectionString = ResolveCirclesConnectionString(builder.Configuration);

static string ResolveCirclesConnectionString(IConfiguration config)
{
    var configured = config.GetConnectionString("Circles");
    if (!string.IsNullOrWhiteSpace(configured))
        return configured;

    var azure = config["AZURE_SQL_CONNECTIONSTRING"]
                ?? Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
    if (!string.IsNullOrWhiteSpace(azure))
        return azure;

    return "Server=localhost,1433;Database=circles;User Id=sa;Password=Circles_Str0ng!Pass;TrustServerCertificate=True;Encrypt=True";
}

// Use DbContextFactory for Blazor Server to avoid concurrency issues
builder.Services.AddDbContextFactory<CirclesDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// Also register scoped DbContext for services that expect it
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<CirclesDbContext>>().CreateDbContext());

builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<CirclesQueryService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<SiteAdminService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpClient<ICalendarSyncService, LagetSeCalendarSyncService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// ---- Migrate + seed on startup --------------------------------------------
// Safe to run alongside the API (both are idempotent) so the web app also
// works standalone against a fresh database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CirclesDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

// ---- HTTP pipeline ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// ---- Auth endpoints --------------------------------------------------------
// Plain form-post endpoints. They run in a real HTTP request context (not an
// interactive circuit), so they can write the auth cookie — the standard
// pattern for signing in from a Blazor Server app.
app.MapPost("/auth/login", async (
    HttpContext http,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl,
    AuthService auth) =>
{
    var account = await auth.ValidateCredentialsAsync(email ?? "", password ?? "");
    if (account is null)
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl ?? "/hem")}");

    // Reload with the linked person for name/pid claims.
    var full = await auth.GetAccountAsync(account.Id);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CookieClaims.Build(full!),
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IsPersistent = true });

    // Site admins land on the platform overview; ordinary users on their home.
    if (full!.IsSiteAdmin && string.IsNullOrWhiteSpace(returnUrl))
        return Results.LocalRedirect("/site");

    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/hem" : returnUrl);
});

// ---- TEMPORARY login diagnostic (remove after troubleshooting) --------------
// Reports the truth about the site-admin account in whatever database THIS app
// instance is actually connected to. Leaks no secret: only the connected server
// /database name, whether the account exists, its site-admin flag, the stored
// hash length/prefix, and whether the documented bootstrap password verifies.
// Open /auth/diag in the browser once, then this block should be deleted.
app.MapGet("/auth/diag", async (
    CirclesDbContext db,
    IPasswordHasher hasher) =>
{
    const string email = "stefan@veum.se";
    const string bootstrapPassword = "Leksand!";

    var account = await db.UserAccounts
        .FirstOrDefaultAsync(u => u.Email == email);

    var totalAccounts = await db.UserAccounts.CountAsync();

    return Results.Json(new
    {
        // Which database is this instance really talking to?
        db.Database.GetDbConnection().DataSource,
        Database = db.Database.GetDbConnection().Database,
        canConnect = await db.Database.CanConnectAsync(),
        totalAccounts,

        // The account itself.
        accountExists = account is not null,
        isSiteAdmin = account?.IsSiteAdmin,
        personIdIsNull = account?.PersonId is null,
        hashLength = account?.PasswordHash?.Length,
        hashPrefix = account?.PasswordHash is { Length: >= 7 }
            ? account.PasswordHash[..7]
            : account?.PasswordHash,

        // Does the documented password verify against the STORED hash?
        bootstrapPasswordVerifies = account?.PasswordHash is { Length: > 0 }
            && hasher.Verify(bootstrapPassword, account.PasswordHash)
    });
});

// ---- Organization registration wizard --------------------------------------
// Creates a new organization, its admin account and its teams, then signs the
// new admin in. Posted from the final step of the /registrera wizard.
app.MapPost("/auth/register-organization", async (
    HttpContext http,
    [FromForm] string orgName,
    [FromForm] string adminFirstName,
    [FromForm] string adminLastName,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? teams,
    OnboardingService onboarding,
    AuthService auth) =>
{
    var teamNames = (teams ?? "")
        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

    var result = await onboarding.RegisterOrganizationAsync(new OnboardingRequest(
        orgName, adminFirstName, adminLastName, email, password, teamNames));

    if (!result.Succeeded || result.AccountId is not { } accountId)
        return Results.Redirect($"/registrera?error={Uri.EscapeDataString(result.Error ?? "Registreringen misslyckades.")}");

    var full = await auth.GetAccountAsync(accountId);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CookieClaims.Build(full!),
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IsPersistent = true });

    return Results.LocalRedirect("/admin");
});

app.MapPost("/auth/magic-link", async (
    HttpContext http,
    [FromForm] string email,
    AuthService auth) =>
{
    var token = await auth.CreateMagicLinkAsync(email ?? "");
    // Dev convenience: echo the token back so the flow is testable without a
    // real email provider. In production this would be emailed out of band and
    // the redirect would show a generic "check your inbox" message.
    var dev = app.Environment.IsDevelopment() && token is not null;
    return Results.Redirect(dev
        ? $"/login?sent=1&devToken={token}"
        : "/login?sent=1");
});

app.MapGet("/auth/magic-link/consume", async (
    HttpContext http,
    string token,
    AuthService auth) =>
{
    var account = await auth.ConsumeMagicLinkAsync(token ?? "");
    if (account is null)
        return Results.Redirect("/login?error=2");

    var full = await auth.GetAccountAsync(account.Id);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CookieClaims.Build(full!),
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IsPersistent = true });

    return Results.LocalRedirect("/hem");
}).DisableAntiforgery(); // GET link from email; no form token available.

app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

// ---- External OAuth (Google / Microsoft) -----------------------------------
// Step 1: challenge the chosen provider. The provider redirects back to its
// CallbackPath (/signin-google or /signin-microsoft) which signs into the
// intermediate "circles.ext" cookie, then forwards to /auth/external-callback.
app.MapGet("/auth/external-login", (HttpContext http, string provider) =>
{
    var scheme = provider switch
    {
        "Google" => "Google",
        "Microsoft" => "Microsoft",
        _ => null
    };
    if (scheme is null)
        return Results.Redirect("/login?error=external");

    var props = new AuthenticationProperties
    {
        RedirectUri = $"/auth/external-callback?provider={scheme}"
    };
    return Results.Challenge(props, new[] { scheme });
});

// Step 2: read the external identity from "circles.ext", match its email to an
// EXISTING account, and (only then) issue the real "circles.auth" session.
app.MapGet("/auth/external-callback", async (HttpContext http, string? provider, AuthService auth) =>
{
    var result = await http.AuthenticateAsync(ExternalScheme);
    // Always clear the temporary external cookie once we've read it.
    await http.SignOutAsync(ExternalScheme);

    if (!result.Succeeded || result.Principal is null)
        return Results.Redirect("/login?error=external");

    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value
                ?? result.Principal.FindFirst("email")?.Value;

    if (string.IsNullOrWhiteSpace(email))
        return Results.Redirect("/login?error=external");

    var account = await auth.GetAccountByEmailAsync(email);
    if (account is null)
        return Results.Redirect($"/login?error=external_not_found&email={Uri.EscapeDataString(email)}");

    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CookieClaims.Build(account),
        new AuthenticationProperties { IsPersistent = true });

    return Results.LocalRedirect("/hem");
}).DisableAntiforgery(); // GET redirect back from provider; no form token.

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Flags which external OAuth providers are configured, so the Login page can
/// conditionally render the corresponding buttons.
/// </summary>
public record ExternalAuthOptions(bool GoogleEnabled, bool MicrosoftEnabled);
