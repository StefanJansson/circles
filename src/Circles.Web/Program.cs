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
builder.Services
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

builder.Services.AddDbContext<CirclesDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<CirclesQueryService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<EventService>();
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

    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/hem" : returnUrl);
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
