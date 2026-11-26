using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ShopSphere.Identity.Application.Auth;
using ShopSphere.Identity.Domain.Users;

namespace ShopSphere.Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "shopsphere";
    public string Audience { get; set; } = "shopsphere-clients";
    /// <summary>HMAC secret, at least 32 chars. Set via configuration/env, never commit a real one.</summary>
    public string Secret { get; set; } = "dev-only-secret-change-me-0123456789abcdef";
    public int ExpiryMinutes { get; set; } = 120;
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(JwtOptions options) => _options = options;

    public TokenResult Create(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
