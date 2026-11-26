using ShopSphere.Identity.Domain.Users;

namespace ShopSphere.Identity.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    TokenResult Create(User user);
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresOnUtc);
