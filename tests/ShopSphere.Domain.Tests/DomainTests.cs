using FluentAssertions;
using ShopSphere.BuildingBlocks.Domain;
using ShopSphere.Cart.Domain.Carts;
using ShopSphere.Inventory.Domain.Stock;
using ShopSphere.Payment.Domain.Payments;

namespace ShopSphere.Domain.Tests;

public class StockItemTests
{
    [Fact]
    public void Reserve_moves_units_from_available_to_reserved()
    {
        var s = StockItem.Create(Guid.NewGuid(), 10);
        s.Reserve(4);
        s.Available.Should().Be(6);
        s.Reserved.Should().Be(4);
    }

    [Fact]
    public void Reserve_more_than_available_is_rejected()
    {
        var s = StockItem.Create(Guid.NewGuid(), 2);
        s.CanReserve(3).Should().BeFalse();
        var act = () => s.Reserve(3);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Release_returns_units_to_available()
    {
        var s = StockItem.Create(Guid.NewGuid(), 5);
        s.Reserve(5);
        s.Release(5);
        s.Available.Should().Be(5);
        s.Reserved.Should().Be(0);
    }

    [Fact]
    public void Commit_consumes_reserved_units()
    {
        var s = StockItem.Create(Guid.NewGuid(), 5);
        s.Reserve(3);
        s.Commit(3);
        s.Reserved.Should().Be(0);
        s.Available.Should().Be(2);
    }
}

public class PaymentRecordTests
{
    [Fact]
    public void New_payment_is_pending()
    {
        var p = PaymentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "EGP");
        p.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Failure_records_reason()
    {
        var p = PaymentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "EGP");
        p.MarkFailed("declined");
        p.Status.Should().Be(PaymentStatus.Failed);
        p.FailureReason.Should().Be("declined");
    }

    [Fact]
    public void Success_marks_succeeded()
    {
        var p = PaymentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "EGP");
        p.MarkSucceeded();
        p.Status.Should().Be(PaymentStatus.Succeeded);
    }
}

public class ShoppingCartTests
{
    private static CartLine Line(Guid id, int qty, decimal price = 10m) => new(id, "P", price, "", qty);

    [Fact]
    public void Adding_same_product_increases_quantity()
    {
        var id = Guid.NewGuid();
        var c = new ShoppingCart(Guid.NewGuid());
        c.AddOrIncrease(Line(id, 1));
        c.AddOrIncrease(Line(id, 2));
        c.Lines.Should().ContainSingle().Which.Quantity.Should().Be(3);
    }

    [Fact]
    public void Total_sums_line_totals()
    {
        var c = new ShoppingCart(Guid.NewGuid());
        c.AddOrIncrease(Line(Guid.NewGuid(), 2, 10m));
        c.AddOrIncrease(Line(Guid.NewGuid(), 1, 5m));
        c.Total.Should().Be(25m);
    }

    [Fact]
    public void Zero_quantity_removes_line()
    {
        var id = Guid.NewGuid();
        var c = new ShoppingCart(Guid.NewGuid());
        c.AddOrIncrease(Line(id, 2));
        c.SetQuantity(id, 0);
        c.Lines.Should().BeEmpty();
    }
}
