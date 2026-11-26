using FluentValidation;
using MediatR;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Contracts.Events;
using ShopSphere.Identity.Domain.Users;

namespace ShopSphere.Identity.Application.Auth;

public sealed record AuthResponse(Guid UserId, string Email, string UserName, string Role, string AccessToken, DateTime ExpiresOnUtc);

public sealed record RegisterCommand(string Email, string UserName, string Password) : IRequest<AuthResponse>;
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Password).MinimumLength(8).Matches("[0-9]").WithMessage("Password needs at least one digit.");
    }
}

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public RegisterHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _users = users; _hasher = hasher; _tokens = tokens; _uow = uow; _events = events;
    }

    public async Task<AuthResponse> Handle(RegisterCommand c, CancellationToken ct)
    {
        if (await _users.GetByEmailAsync(c.Email, ct) is not null) throw new DomainException("Email is already registered.");

        var user = User.Register(c.Email, c.UserName, _hasher.Hash(c.Password));
        await _users.AddAsync(user, ct);
        _events.Collect(new UserRegisteredIntegrationEvent(user.Id, user.Email, user.UserName));
        await _uow.SaveChangesAsync(ct);

        var token = _tokens.Create(user);
        return new AuthResponse(user.Id, user.Email, user.UserName, user.Role.ToString(), token.AccessToken, token.ExpiresOnUtc);
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users; _hasher = hasher; _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(LoginCommand c, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(c.Email, ct);
        if (user is null || !_hasher.Verify(c.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        var token = _tokens.Create(user);
        return new AuthResponse(user.Id, user.Email, user.UserName, user.Role.ToString(), token.AccessToken, token.ExpiresOnUtc);
    }
}
