using Circles.API.Auth;
using Circles.Application.Authentication;
using Circles.Application.Authorization;
using Circles.Application.Services;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Circles.Infrastructure.Security;
using Circles.Infrastructure.Seeding;
using Circles.Infrastructure.Sync;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---- Authentication (JWT bearer) -----------------------------------------
// Signing key comes from configuration. A development fallback keeps the local
// prototype runnable out of the box; production MUST supply Auth__JwtSigningKey.
var configuredKey = builder.Configuration["Auth:JwtSigningKey"];
var jwtSigningKey = string.IsNullOrWhiteSpace(configuredKey)
    ? "dev-only-insecure-signing-key-change-me-please-32chars-minimum!!"
    : configuredKey;
builder.Configuration["Auth:JwtSigningKey"] = jwtSigningKey;

builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtSigningKey);
builder.Services.AddAuthorization();

// ---- Services -------------------------------------------------------------
// FastEndpoints (REPR pattern). Endpoints live under Features/ as vertical slices.
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.EnableJWTBearerAuth = true;
    o.DocumentSettings = s =>
    {
        s.DocumentName = "v1";
        s.Title = "Circles API";
        s.Version = "v1";
    };
});

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

// Application services.
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<CirclesQueryService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpClient<ICalendarSyncService, LagetSeCalendarSyncService>();

// Authentication / onboarding services.
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

// ---- Migrate + seed on startup -------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CirclesDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

// ---- HTTP pipeline --------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    // Serialize enums as their string names for readable, stable API output.
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

// Exposed so integration tests / tooling can reference the entry point.
public partial class Program { }
