using ShopSphere.BuildingBlocks.Domain;

namespace ShopSphere.Identity.Domain.Users;

public enum UserRole { Customer = 0, Admin = 1 }

public sealed class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private User() { }

    public static User Register(string email, string userName, string passwordHash, UserRole role = UserRole.Customer)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new DomainException("A valid email is required.");
        if (string.IsNullOrWhiteSpace(userName)) throw new DomainException("User name is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        return new User
        {
            Email = email.Trim().ToLowerInvariant(), UserName = userName.Trim(), PasswordHash = passwordHash,
            Role = role, CreatedOnUtc = DateTime.UtcNow
        };
    }
}
