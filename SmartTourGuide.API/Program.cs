using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTourGuide.API.Middleware;
using SmartTourGuide.Business.Interfaces;
using SmartTourGuide.Business.Options;
using SmartTourGuide.Business.Services;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Data.Repositories;
using SmartTourGuide.Data.Repositories.Interfaces;
using SmartTourGuide.Data.SeedData;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Connection string
// ========================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' " +
        "was not found.");

// ========================================
// JWT configuration
// ========================================

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

var jwtOptions =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
{
    throw new InvalidOperationException(
        "JWT issuer was not configured.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "JWT audience was not configured.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "JWT signing key was not configured. Set Jwt:Key with user-secrets " +
        "in development or Jwt__Key in the deployment environment.");
}

if (jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least " +
        "32 characters.");
}

if (jwtOptions.RegisteredUserExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "Registered user token expiration must " +
        "be greater than zero.");
}

if (jwtOptions.GuestExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "Guest token expiration must be " +
        "greater than zero.");
}

// ========================================
// ASP.NET Core services
// ========================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

// ========================================
// Database configuration
// ========================================

builder.Services.AddDbContext<
    SmartTourGuideDbContext>(
    options =>
    {
        options.UseSqlServer(
            connectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay:
                        TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
    });

// ========================================
// Application dependencies
// ========================================

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<
    ITokenService,
    TokenService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

// ========================================
// Authentication and authorization
// ========================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromSeconds(30),

                NameClaimType =
                    ClaimTypes.NameIdentifier,

                RoleClaimType =
                    ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

// ========================================
// Build application
// ========================================

var app = builder.Build();

// ========================================
// Development initialization
// ========================================

if (app.Environment.IsDevelopment())
{
    using var scope =
        app.Services.CreateScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                SmartTourGuideDbContext>();

    await dbContext.Database.MigrateAsync();

    await DatabaseSeeder.SeedAsync(
        dbContext);

    app.MapOpenApi();
}

// ========================================
// HTTP middleware pipeline
// ========================================

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

// ========================================
// Endpoints
// ========================================

app.MapHealthChecks("/health/live");

app.MapGet("/health/ready", async (
    SmartTourGuide.Data.Context.SmartTourGuideDbContext db,
    CancellationToken cancellationToken) =>
{
    IResult response;

    try
    {
        var canConnect = await db.Database.CanConnectAsync(
            cancellationToken);

        if (canConnect)
        {
            response = Results.Ok(new
            {
                status = "ready",
                database = "reachable"
            });
        }
        else
        {
            response = Results.Json(
                new
                {
                    status = "not_ready",
                    database = "unavailable"
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
    catch
    {
        response = Results.Json(
            new
            {
                status = "not_ready",
                database = "unavailable"
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return response;
});

app.MapGet("/tour/{accessCode}", (string accessCode) =>
    Results.Content(
        $"<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><meta http-equiv='refresh' content='0;url=/?code={Uri.EscapeDataString(accessCode)}'></head><body>Đang mở thuyết minh...</body></html>",
        "text/html; charset=utf-8"));

app.MapControllers();

// This keeps the web server running.
app.Run();
