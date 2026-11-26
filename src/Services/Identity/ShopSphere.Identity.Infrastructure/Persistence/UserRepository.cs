using Microsoft.EntityFrameworkCore;
using ShopSphere.Identity.Domain.Users;

namespace ShopSphere.Identity.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;
    public UserRepository(IdentityDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct) => await _db.Users.AddAsync(user, ct);
}
