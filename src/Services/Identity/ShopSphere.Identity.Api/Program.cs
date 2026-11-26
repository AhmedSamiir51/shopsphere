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
app.UseShopSphereApi();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

var auth = app.MapGroup("/api/auth");
auth.MapPost("/register", async (RegisterCommand c, ISender m, CancellationToken ct) => Results.Ok(await m.Send(c, ct)));
auth.MapPost("/login", async (LoginCommand c, ISender m, CancellationToken ct) => Results.Ok(await m.Send(c, ct)));
auth.MapGet("/me", (ClaimsPrincipal u) => Results.Ok(new
{
    id = u.FindFirst("sub")?.Value, email = u.FindFirst("email")?.Value, name = u.FindFirst("name")?.Value,
    role = u.FindFirst(ClaimTypes.Role)?.Value
})).RequireAuthorization();

app.Run();

public partial class Program;
