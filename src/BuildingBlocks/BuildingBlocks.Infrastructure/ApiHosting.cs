using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.BuildingBlocks.Infrastructure;

/// <summary>Cross-cutting API plumbing shared by every service: JWT auth, CORS, health,
/// global error handling (domain/validation errors become RFC 7807 problem details).</summary>
public static class ApiHosting
{
    public static IServiceCollection AddShopSphereApi(this IServiceCollection services, IConfiguration config)
    {
        var secret = config["Jwt:Secret"] ?? "dev-only-secret-change-me-0123456789abcdef";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = config["Jwt:Issuer"] ?? "shopsphere",
                ValidAudience = config["Jwt:Audience"] ?? "shopsphere-clients",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                NameClaimType = "name"
            };
            o.MapInboundClaims = false; // keep "sub" as "sub"
        });
        services.AddAuthorization();

        services.AddCors(o => o.AddDefaultPolicy(p => p
            .WithOrigins((config["Cors:Origins"] ?? "http://localhost:4200").Split(',', StringSplitOptions.RemoveEmptyEntries))
            .AllowAnyHeader().AllowAnyMethod()));

        // Brute-force protection: "auth" policy = 10 attempts/minute per client IP.
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = int.Parse(config["RateLimit:AuthPerMinute"] ?? "10"), Window = TimeSpan.FromMinutes(1) }));
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        return services;
    }

    public static WebApplication UseShopSphereApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        return app;
    }

    /// <summary>Creates the service database/schema if missing. Retries while Postgres boots.</summary>
    public static async Task EnsureDatabaseAsync<TContext>(this WebApplication app) where TContext : DbContext
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception ex) when (attempt < 15)
            {
                app.Logger.LogWarning("Database not ready ({Message}), retry {Attempt}/15", ex.Message, attempt);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        ProblemDetails problem;
        switch (ex)
        {
            case ValidationException v:
                problem = new ValidationProblemDetails(v.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                { Status = StatusCodes.Status400BadRequest, Title = "Validation failed" };
                break;
            case DomainException d:
                problem = new ProblemDetails { Status = d.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest, Title = d.Message };
                break;
            default:
                return false;
        }
        ctx.Response.StatusCode = problem.Status!.Value;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}

public static class UserClaims
{
    public static Guid GetUserId(this System.Security.Claims.ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : throw new DomainException("Unauthenticated.");
}
