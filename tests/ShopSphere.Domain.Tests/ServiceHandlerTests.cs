using FluentAssertions;
using NSubstitute;
using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Catalog.Application.Products;
using ShopSphere.Catalog.Domain.Products;
using ShopSphere.Contracts;
using ShopSphere.Contracts.Events;
using ShopSphere.Identity.Application.Auth;
using ShopSphere.Identity.Domain.Users;
using ShopSphere.Notification.Application.Handlers;
using ShopSphere.Notification.Domain.Notifications;
using NotificationEntity = ShopSphere.Notification.Domain.Notifications.Notification;

namespace ShopSphere.Domain.Tests;

public class CatalogHandlerTests
{
    private readonly IProductRepository _repo = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventCollector _events = Substitute.For<IIntegrationEventCollector>();
    private readonly ICatalogCache _cache = Substitute.For<ICatalogCache>();

    private static Product NewProduct(decimal price = 100m) =>
        Product.Create("Lamp", "lmp-1", "desc", price, "egp", "img", Guid.NewGuid());

    [Fact]
    public async Task ChangePrice_publishes_event_and_invalidates_cache()
    {
        var p = NewProduct(100m);
        _repo.GetAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        await new ChangePriceHandler(_repo, _uow, _events, _cache).Handle(new ChangePriceCommand(p.Id, 150m), default);

        p.Price.Should().Be(150m);
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is ProductPriceChangedIntegrationEvent));
        await _cache.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePrice_to_same_price_publishes_nothing()
    {
        var p = NewProduct(100m);
        _repo.GetAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        await new ChangePriceHandler(_repo, _uow, _events, _cache).Handle(new ChangePriceCommand(p.Id, 100m), default);
        _events.DidNotReceive().Collect(Arg.Any<IntegrationEvent>());
    }

    [Fact]
    public async Task ChangePrice_unknown_product_throws()
    {
        _repo.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var act = () => new ChangePriceHandler(_repo, _uow, _events, _cache).Handle(new ChangePriceCommand(Guid.NewGuid(), 5m), default);
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Discontinue_marks_inactive_and_publishes()
    {
        var p = NewProduct();
        _repo.GetAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        await new DiscontinueProductHandler(_repo, _uow, _events, _cache).Handle(new DiscontinueProductCommand(p.Id), default);
        p.IsActive.Should().BeFalse();
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is ProductDiscontinuedIntegrationEvent));
    }

    [Fact]
    public async Task GetProduct_serves_from_cache_without_hitting_the_repository()
    {
        var p = NewProduct();
        var dto = new ProductDto(p.Id, p.Name, p.Sku, p.Description, p.Price, p.Currency, p.ImageUrl, p.CategoryId, 0, 0);
        _cache.GetAsync<ProductDto>($"product:{p.Id}", Arg.Any<CancellationToken>()).Returns(dto);
        var result = await new GetProductHandler(_repo, _cache).Handle(new GetProductQuery(p.Id), default);
        result.Should().BeSameAs(dto);
        await _repo.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProduct_on_miss_reads_repository_and_populates_cache()
    {
        var p = NewProduct();
        _cache.GetAsync<ProductDto>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ProductDto?)null);
        _repo.GetAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        var result = await new GetProductHandler(_repo, _cache).Handle(new GetProductQuery(p.Id), default);
        result.Sku.Should().Be("LMP-1");
        await _cache.Received(1).SetAsync($"product:{p.Id}", Arg.Any<ProductDto>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "SKU", 10, "EGP")]
    [InlineData("N", "", 10, "EGP")]
    [InlineData("N", "SKU", 0, "EGP")]
    [InlineData("N", "SKU", 10, "EG")]
    public void Create_validator_rejects_bad_input(string name, string sku, decimal price, string cur) =>
        new CreateProductValidator().Validate(new CreateProductCommand(name, sku, "", price, cur, "", Guid.NewGuid()))
            .IsValid.Should().BeFalse();
}

public class IdentityHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventCollector _events = Substitute.For<IIntegrationEventCollector>();

    public IdentityHandlerTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns(c => "hash:" + c.Arg<string>());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(c => "hash:" + c.ArgAt<string>(0) == c.ArgAt<string>(1));
        _tokens.Create(Arg.Any<User>()).Returns(new TokenResult("tok", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task Register_creates_customer_publishes_event_and_returns_token()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var res = await new RegisterHandler(_users, _hasher, _tokens, _uow, _events)
            .Handle(new RegisterCommand("A@B.com", "Ann", "secret123"), default);
        res.Email.Should().Be("a@b.com");
        res.Role.Should().Be("Customer");
        res.AccessToken.Should().Be("tok");
        _events.Received(1).Collect(Arg.Is<IntegrationEvent>(e => e is UserRegisteredIntegrationEvent));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_duplicate_email_is_rejected()
    {
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(User.Register("a@b.com", "Ann", "h"));
        var act = () => new RegisterHandler(_users, _hasher, _tokens, _uow, _events)
            .Handle(new RegisterCommand("a@b.com", "Ann", "secret123"), default);
        await act.Should().ThrowAsync<DomainException>().WithMessage("*already registered*");
    }

    [Fact]
    public async Task Login_with_wrong_password_fails_with_generic_message()
    {
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(User.Register("a@b.com", "Ann", "hash:right1234"));
        var act = () => new LoginHandler(_users, _hasher, _tokens).Handle(new LoginCommand("a@b.com", "wrong1234"), default);
        await act.Should().ThrowAsync<DomainException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_unknown_email_gives_same_generic_message()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var act = () => new LoginHandler(_users, _hasher, _tokens).Handle(new LoginCommand("x@y.com", "whatever1"), default);
        await act.Should().ThrowAsync<DomainException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_with_correct_password_returns_token()
    {
        _users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(User.Register("a@b.com", "Ann", "hash:right1234"));
        var res = await new LoginHandler(_users, _hasher, _tokens).Handle(new LoginCommand("a@b.com", "right1234"), default);
        res.AccessToken.Should().Be("tok");
    }

    [Theory]
    [InlineData("a@b.com", "short1", false)]
    [InlineData("a@b.com", "nodigitshere", false)]
    [InlineData("not-an-email", "longenough1", false)]
    [InlineData("a@b.com", "longenough1", true)]
    public void Register_validator(string email, string pwd, bool valid) =>
        new RegisterValidator().Validate(new RegisterCommand(email, "Ann", pwd)).IsValid.Should().Be(valid);

    [Fact]
    public void User_register_normalises_email_and_requires_fields()
    {
        User.Register(" Ann@B.COM ", "Ann", "h").Email.Should().Be("ann@b.com");
        var act = () => User.Register("nope", "Ann", "h");
        act.Should().Throw<DomainException>();
    }
}

public class NotificationHandlerTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Order_confirmed_stores_in_app_notification_and_sends_email()
    {
        var userId = Guid.NewGuid();
        var evt = new OrderConfirmedIntegrationEvent(Guid.NewGuid(), userId, 3499m, "EGP");
        await new OrderConfirmedNotifier(_repo, _email, _uow).Handle(evt, default);

        await _repo.Received(1).AddAsync(Arg.Is<NotificationEntity>(n =>
            n.UserId == userId && n.Subject == "Order confirmed" && n.Body.Contains("3499.00")), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(userId, "Order confirmed", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Order_cancelled_includes_reason()
    {
        var userId = Guid.NewGuid();
        await new OrderCancelledNotifier(_repo, _email, _uow)
            .Handle(new OrderCancelledIntegrationEvent(Guid.NewGuid(), userId, "Card declined"), default);
        await _email.Received(1).SendAsync(userId, "Order cancelled", Arg.Is<string>(b => b.Contains("Card declined")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task User_registered_sends_welcome()
    {
        var userId = Guid.NewGuid();
        await new UserRegisteredNotifier(_repo, _email, _uow)
            .Handle(new UserRegisteredIntegrationEvent(userId, "a@b.com", "Ann"), default);
        await _email.Received(1).SendAsync(userId, "Welcome to ShopSphere", Arg.Is<string>(b => b.Contains("Ann")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Notification_requires_subject_and_can_be_marked_read_once()
    {
        var act = () => NotificationEntity.Create(Guid.NewGuid(), NotificationChannel.InApp, " ", "b");
        act.Should().Throw<DomainException>();
        var n = NotificationEntity.Create(Guid.NewGuid(), NotificationChannel.InApp, "s", "b");
        n.MarkRead(); var first = n.ReadOnUtc; n.MarkRead();
        n.ReadOnUtc.Should().Be(first);
    }
}
