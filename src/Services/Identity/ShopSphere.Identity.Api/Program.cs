using System.Reflection;
using MediatR;
using ShopSphere.BuildingBlocks.Application;
using ShopSphere.BuildingBlocks.EventBus;
using ShopSphere.BuildingBlocks.Infrastructure;
using System.Security.Claims;
using ShopSphere.Identity.Application.Auth;
using ShopSphere.Identity.Domain.Users;
using ShopSphere.Identity.Infrastructure.Persistence;
using ShopSphere.Identity.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddShopSphereApi(builder.Configuration);
services.AddOpenApi();
services.AddApplicationCore(typeof(RegisterCommand).Assembly);
services.AddShopSphereDbContext<IdentityDbContext>(builder.Configuration);
services.AddShopSphereEventBus(builder.Configuration, "identity");
services.AddScoped<IUserRepository, UserRepository>();
services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
var jwt = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwt);
services.AddSingleton(jwt);
services.AddSingleton<ITokenService, JwtTokenService>();

var app = builder.Build();
await app.EnsureDatabaseAsync<IdentityDbContext>();
// Demo admin account (override with Admin:Email / Admin:Password; change in any real deployment).
using (var scope = app.Services.CreateScope())
{
    var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var email = app.Configuration["Admin:Email"] ?? "admin@shopsphere.dev";
    if (await users.GetByEmailAsync(email, default) is null)
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await users.AddAsync(User.Register(email, "Admin", hasher.Hash(app.Configuration["Admin:Password"] ?? "Admin123!"), UserRole.Admin), default);
        await scope.ServiceProvider.GetRequiredService<ShopSphere.BuildingBlocks.Application.Abstractions.IUnitOfWork>().SaveChangesAsync(default);
    }
}
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var auth = app.MapGroup("/api/auth");
auth.MapPost("/register", async (RegisterCommand c, ISender m, CancellationToken ct) => Results.Ok(await m.Send(c, ct))).RequireRateLimiting("auth");
auth.MapPost("/login", async (LoginCommand c, ISender m, CancellationToken ct) => Results.Ok(await m.Send(c, ct))).RequireRateLimiting("auth");
auth.MapGet("/me", (ClaimsPrincipal u) => Results.Ok(new
{
    id = u.FindFirst("sub")?.Value, email = u.FindFirst("email")?.Value, name = u.FindFirst("name")?.Value,
    role = u.FindFirst(ClaimTypes.Role)?.Value
})).RequireAuthorization();

app.Run();

public partial class Program;
